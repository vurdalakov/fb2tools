namespace Vurdalakov
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;

    public static class Fb2InfoReader
    {
        private const String XlinkNamespace = "http://www.w3.org/1999/xlink";

        // FB2 files are often windows-1251 or other legacy code pages
        static Fb2InfoReader() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Reads title-info only and stops at its end, so large files are read quickly
        public static Fb2Info Read(Stream stream) => Fb2InfoReader.Read(stream, false);

        // With readCover, also reads document-info/id and the cover image (stored at the end of the file)
        public static Fb2Info Read(Stream stream, Boolean readCover)
        {
            ArgumentNullException.ThrowIfNull(stream);

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                IgnoreWhitespace = true, // as XDocument.Load does, so that annotation XML has no line breaks between elements
                XmlResolver = null,
            };

            var info = new Fb2Info();

            String coverId = null;
            var titleInfoSeen = false;
            var titleInfoDepth = -1;
            var documentInfoDepth = -1;

            using (var reader = XmlReader.Create(stream, settings))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.EndElement)
                    {
                        if (reader.Depth == titleInfoDepth)
                        {
                            titleInfoDepth = -1;

                            if (!readCover)
                            {
                                break;
                            }
                        }
                        else if (reader.Depth == documentInfoDepth)
                        {
                            documentInfoDepth = -1;
                        }
                        else if (readCover && (coverId == null) && (reader.Depth == 1) && (reader.LocalName == "description"))
                        {
                            break; // no cover, nothing more to read
                        }

                        continue;
                    }

                    if (reader.NodeType != XmlNodeType.Element)
                    {
                        continue;
                    }

                    var depth = reader.Depth;

                    // Outside title-info and document-info
                    if ((titleInfoDepth < 0) && (documentInfoDepth < 0))
                    {
                        if (!titleInfoSeen && (reader.LocalName == "title-info"))
                        {
                            titleInfoSeen = true;
                            titleInfoDepth = depth;
                        }
                        else if (readCover && (reader.LocalName == "document-info"))
                        {
                            documentInfoDepth = depth;
                        }
                        else if (readCover && (coverId != null) && (reader.LocalName == "binary") && (reader.GetAttribute("id") == coverId))
                        {
                            Fb2InfoReader.ReadCoverImage(reader, info);
                            break;
                        }

                        continue;
                    }

                    // ReadElement (ReadSubtree) leaves the reader on the element end, so the loop continues with the next node
                    if ((titleInfoDepth >= 0) && (depth == (titleInfoDepth + 1)))
                    {
                        switch (reader.LocalName)
                        {
                            case "author":
                                info.Author ??= Fb2InfoReader.GetPersonName(Fb2InfoReader.ReadElement(reader));
                                break;

                            case "translator":
                                info.Translator ??= Fb2InfoReader.GetPersonName(Fb2InfoReader.ReadElement(reader));
                                break;

                            case "book-title":
                                info.Title ??= Fb2InfoReader.Clean(Fb2InfoReader.ReadElement(reader).Value);
                                break;

                            case "lang":
                                info.Language ??= Fb2InfoReader.Clean(Fb2InfoReader.ReadElement(reader).Value);
                                break;

                            case "annotation":
                                if (info.Annotation == null)
                                {
                                    var annotation = Fb2InfoReader.ReadElement(reader);
                                    info.Annotation = String.Concat(annotation.Nodes().Select(n => n.ToString()));
                                }

                                break;

                            case "sequence":
                                if (info.Series == null)
                                {
                                    var sequence = Fb2InfoReader.ReadElement(reader);
                                    info.Series = Fb2InfoReader.Clean(sequence.Attributes().FirstOrDefault(a => a.Name.LocalName == "name")?.Value);
                                    info.SeriesNumber = Fb2InfoReader.Clean(sequence.Attributes().FirstOrDefault(a => a.Name.LocalName == "number")?.Value);
                                }

                                break;

                            case "coverpage":
                                if (readCover && (coverId == null))
                                {
                                    var image = Fb2InfoReader.ReadElement(reader).Descendants().FirstOrDefault(e => e.Name.LocalName == "image");
                                    var href = image?.Attributes().FirstOrDefault(a => (a.Name.LocalName == "href") && (a.Name.NamespaceName == Fb2InfoReader.XlinkNamespace))?.Value;
                                    coverId = href?.TrimStart('#');
                                }

                                break;
                        }
                    }
                    else if ((documentInfoDepth >= 0) && (depth == (documentInfoDepth + 1)) && (reader.LocalName == "id"))
                    {
                        info.DocumentId ??= Fb2InfoReader.Clean(Fb2InfoReader.ReadElement(reader).Value);
                    }
                }
            }

            return info;
        }

        private static void ReadCoverImage(XmlReader reader, Fb2Info info)
        {
            info.CoverContentType = reader.GetAttribute("content-type") ?? "image/jpeg";

            var text = Fb2InfoReader.ReadElement(reader).Value;

            try
            {
                info.CoverImage = Convert.FromBase64String(text.Trim());
            }
            catch (FormatException exception)
            {
                info.CoverError = exception.Message;
            }
        }

        private static XElement ReadElement(XmlReader reader)
        {
            using (var subtree = reader.ReadSubtree())
            {
                return XElement.Load(subtree);
            }
        }

        // First name + last name; the middle name is not part of a file name. The nickname is used only if there is no real name.
        private static String GetPersonName(XElement person)
        {
            String Part(String name) => Fb2InfoReader.Clean(person.Elements().FirstOrDefault(e => e.Name.LocalName == name)?.Value);

            var name = Fb2InfoReader.Clean($"{Part("first-name")} {Part("last-name")}");

            return name ?? Part("nickname");
        }

        // Collapses white space (FB2 titles often contain line breaks); empty text becomes null
        private static String Clean(String text)
        {
            if (text == null)
            {
                return null;
            }

            var clean = String.Join(' ', text.Split((Char[])null, StringSplitOptions.RemoveEmptyEntries));

            return (clean.Length == 0) ? null : clean;
        }
    }
}
