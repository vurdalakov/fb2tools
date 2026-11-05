namespace Vurdalakov
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Linq;

    // Writes changed title-info fields (author, translator, title, series) back into an FB2 file or the .fb2 inside a .fb2.zip.
    // Only the title-info element is rewritten, with the same encoding; the rest of the file is not touched.
    public static class Fb2InfoWriter
    {
        static Fb2InfoWriter() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        public static void Update(String filePath, Fb2Info info)
        {
            ArgumentException.ThrowIfNullOrEmpty(filePath);
            ArgumentNullException.ThrowIfNull(info);

            if (filePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                Fb2InfoWriter.UpdateZipFile(filePath, info);
            }
            else
            {
                Fb2InfoWriter.UpdateFile(filePath, info);
            }
        }

        private static void UpdateFile(String filePath, Fb2Info info)
        {
            var original = File.ReadAllBytes(filePath);
            var updated = Fb2InfoWriter.Transform(original, info);

            // Nothing changed: the file is not touched
            if (ReferenceEquals(updated, original))
            {
                return;
            }

            // Write a temporary file first, so that a failure never damages the original
            var tempFilePath = filePath + ".tmp";
            try
            {
                File.WriteAllBytes(tempFilePath, updated);
                File.Move(tempFilePath, filePath, true);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        private static void UpdateZipFile(String zipFilePath, Fb2Info info)
        {
            // Read the book first; if nothing changes, the zip is not touched
            byte[] original;
            using (var archive = ZipFile.OpenRead(zipFilePath))
            {
                var entry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".fb2", StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidDataException("No .fb2 entry in zip.");

                using (var stream = entry.Open())
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    original = memoryStream.ToArray();
                }
            }

            var updated = Fb2InfoWriter.Transform(original, info);
            if (ReferenceEquals(updated, original))
            {
                return;
            }

            var tempFilePath = zipFilePath + ".tmp";
            try
            {
                File.Copy(zipFilePath, tempFilePath, true);

                using (var archive = ZipFile.Open(tempFilePath, ZipArchiveMode.Update))
                {
                    var entry = archive.Entries.First(e => e.FullName.EndsWith(".fb2", StringComparison.OrdinalIgnoreCase));

                    // The entry is changed in place, so its stored name (which may be in a legacy code page) is not touched
                    using (var stream = entry.Open())
                    {
                        stream.SetLength(0);
                        stream.Seek(0, SeekOrigin.Begin);
                        stream.Write(updated, 0, updated.Length);
                    }
                }

                File.Move(tempFilePath, zipFilePath, true);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        // Returns the updated file content. Only the title-info element is replaced; all other bytes of the file stay as they are.
        // Throws if reading the result back does not give the requested values.
        private static byte[] Transform(byte[] original, Fb2Info info)
        {
            var encoding = Fb2InfoWriter.DetectEncoding(original);
            var text = encoding.GetString(original);

            var start = Fb2InfoWriter.FindTitleInfoStart(text);
            if (start < 0)
            {
                throw new InvalidDataException("No title-info element.");
            }

            var end = Fb2InfoWriter.FindTitleInfoEnd(text, start);

            // The title-info is parsed inside a copy of the root start tag, so that it sees the same namespaces
            var rootStartTag = Regex.Match(text, @"<FictionBook(\s[^>]*)?>").Value;
            if (rootStartTag.Length == 0)
            {
                throw new InvalidDataException("No FictionBook element.");
            }

            XDocument document;
            using (var reader = new XmlTextReader(new StringReader(rootStartTag + text.Substring(start, end - start) + "</FictionBook>")))
            {
                reader.DtdProcessing = DtdProcessing.Ignore;
                reader.XmlResolver = null;
                reader.Normalization = false; // keep CR LF as they are
                document = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
            }

            var titleInfo = document.Root.Elements().First();
            var before = titleInfo.ToString(SaveOptions.DisableFormatting);

            Fb2InfoWriter.SetPerson(titleInfo, "author", info.AuthorFirstName, info.AuthorLastName);
            Fb2InfoWriter.SetTitle(titleInfo, info.Title);
            Fb2InfoWriter.SetPerson(titleInfo, "translator", info.TranslatorFirstName, info.TranslatorLastName);
            Fb2InfoWriter.SetSequence(titleInfo, info.Series, info.SeriesNumber);

            if (before == titleInfo.ToString(SaveOptions.DisableFormatting))
            {
                return original; // nothing changed
            }

            // Characters that the file encoding cannot represent are written as character references by the XML writer
            var settings = new XmlWriterSettings
            {
                Encoding = Fb2InfoWriter.WithoutPreamble(encoding),
                OmitXmlDeclaration = true,
                NewLineHandling = NewLineHandling.None,
                Indent = false,
            };

            String updatedXml;
            using (var memoryStream = new MemoryStream())
            {
                using (var writer = XmlWriter.Create(memoryStream, settings))
                {
                    document.Save(writer);
                }

                updatedXml = settings.Encoding.GetString(memoryStream.ToArray());
            }

            var innerStart = updatedXml.IndexOf('>') + 1; // end of the root start tag
            var innerEnd = updatedXml.LastIndexOf("</FictionBook>", StringComparison.Ordinal);
            var updatedTitleInfo = updatedXml.Substring(innerStart, innerEnd - innerStart);

            var updated = encoding.GetBytes(text.Substring(0, start) + updatedTitleInfo + text.Substring(end));

            Fb2InfoWriter.Verify(updated, info);

            return updated;
        }

        // The encoding of the file: byte order mark, else the XML declaration, else UTF-8.
        // Invalid bytes throw, so a file that is not what its declaration says is never rewritten damaged.
        private static Encoding DetectEncoding(byte[] original)
        {
            if ((original.Length >= 3) && (original[0] == 0xEF) && (original[1] == 0xBB) && (original[2] == 0xBF))
            {
                return new UTF8Encoding(true, true);
            }

            if ((original.Length >= 2) && (original[0] == 0xFF) && (original[1] == 0xFE))
            {
                return new UnicodeEncoding(false, true, true);
            }

            if ((original.Length >= 2) && (original[0] == 0xFE) && (original[1] == 0xFF))
            {
                return new UnicodeEncoding(true, true, true);
            }

            var declaration = Encoding.ASCII.GetString(original, 0, Math.Min(original.Length, 256));
            var match = Regex.Match(declaration, @"<\?xml[^>]*encoding\s*=\s*['""]([\w\-]+)['""]", RegexOptions.IgnoreCase);

            return !match.Success || String.Equals(match.Groups[1].Value, "utf-8", StringComparison.OrdinalIgnoreCase)
                ? new UTF8Encoding(false, true)
                : Encoding.GetEncoding(match.Groups[1].Value, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        }

        // The same encoding without the byte order mark, for writing a part of a file
        private static Encoding WithoutPreamble(Encoding encoding)
        {
            switch (encoding.CodePage)
            {
                case 65001:
                    return new UTF8Encoding(false);
                case 1200:
                    return new UnicodeEncoding(false, false);
                case 1201:
                    return new UnicodeEncoding(true, false);
                default:
                    return encoding;
            }
        }

        // The title-info start tag ("<src-title-info" does not match)
        private static Int32 FindTitleInfoStart(String text)
        {
            var match = Regex.Match(text, @"<title-info(?=[\s>/])");

            return match.Success ? match.Index : -1;
        }

        // The index after the end of the title-info element
        private static Int32 FindTitleInfoEnd(String text, Int32 start)
        {
            var startTagEnd = text.IndexOf('>', start);
            if (startTagEnd < 0)
            {
                throw new InvalidDataException("Damaged title-info element.");
            }

            if (text[startTagEnd - 1] == '/')
            {
                return startTagEnd + 1; // <title-info/>
            }

            var close = text.IndexOf("</title-info>", startTagEnd, StringComparison.Ordinal);

            return (close < 0) ? throw new InvalidDataException("Damaged title-info element.") : close + "</title-info>".Length;
        }

        private static void Verify(byte[] updated, Fb2Info expected)
        {
            var actual = Fb2InfoReader.Read(new MemoryStream(updated));

            void Check(String name, String expectedValue, String actualValue)
            {
                if (!String.Equals(Fb2InfoWriter.Normalize(expectedValue), actualValue, StringComparison.Ordinal))
                {
                    throw new InvalidDataException($"Verification failed: {name} was not saved correctly.");
                }
            }

            Check("author first name", expected.AuthorFirstName, actual.AuthorFirstName);
            Check("author last name", expected.AuthorLastName, actual.AuthorLastName);
            Check("title", expected.Title, actual.Title);
            Check("series", expected.Series, actual.Series);
            Check("series number", expected.SeriesNumber, actual.SeriesNumber);
        }

        // The element that the reader treats as "the" author or translator: the first one that has a name
        private static XElement FindPerson(XElement titleInfo, String elementName)
        {
            var persons = titleInfo.Elements().Where(e => e.Name.LocalName == elementName).ToList();

            return persons.FirstOrDefault(p => p.Elements().Any(c => (c.Name.LocalName is "first-name" or "last-name" or "nickname") && (Fb2InfoWriter.Normalize(c.Value) != null))) ?? persons.FirstOrDefault();
        }

        private static void SetPerson(XElement titleInfo, String elementName, String firstName, String lastName)
        {
            firstName = Fb2InfoWriter.Normalize(firstName);
            lastName = Fb2InfoWriter.Normalize(lastName);

            var person = Fb2InfoWriter.FindPerson(titleInfo, elementName);

            if (person == null)
            {
                if ((firstName == null) && (lastName == null))
                {
                    return;
                }

                // Order in title-info: genre, author, book-title, ..., lang, src-lang, translator, sequence
                person = new XElement(titleInfo.Name.Namespace + elementName);

                if (elementName == "author")
                {
                    Fb2InfoWriter.AddAfterLast(titleInfo, person, "genre");
                }
                else
                {
                    Fb2InfoWriter.AddAfterLast(titleInfo, person, "translator", "src-lang", "lang");
                }
            }

            // Order in a person: first-name, middle-name, last-name, nickname, ...
            Fb2InfoWriter.SetChild(person, "first-name", firstName);
            Fb2InfoWriter.SetChild(person, "last-name", lastName, "middle-name", "first-name");

            // A translator without any data is removed
            if ((elementName == "translator") && !person.HasElements)
            {
                person.Remove();
            }
        }

        private static void SetChild(XElement parent, String name, String value, params String[] afterNames)
        {
            var element = parent.Elements().FirstOrDefault(e => e.Name.LocalName == name);

            if (element != null)
            {
                // Unchanged values are left alone, even if the element is empty
                if (Fb2InfoWriter.Normalize(element.Value) == value)
                {
                    return;
                }

                if (value == null)
                {
                    element.Remove();
                }
                else
                {
                    element.Value = value;
                }

                return;
            }

            if (value != null)
            {
                Fb2InfoWriter.AddAfterLast(parent, new XElement(parent.Name.Namespace + name, value), afterNames);
            }
        }

        private static void SetTitle(XElement titleInfo, String title)
        {
            title = Fb2InfoWriter.Normalize(title);

            var element = titleInfo.Elements().FirstOrDefault(e => e.Name.LocalName == "book-title");

            if (element == null)
            {
                if (title != null)
                {
                    Fb2InfoWriter.AddAfterLast(titleInfo, new XElement(titleInfo.Name.Namespace + "book-title", title), "author", "genre");
                }

                return;
            }

            if (Fb2InfoWriter.Normalize(element.Value) != title)
            {
                element.Value = title ?? String.Empty;
            }
        }

        private static void SetSequence(XElement titleInfo, String series, String number)
        {
            series = Fb2InfoWriter.Normalize(series);
            number = Fb2InfoWriter.Normalize(number);

            var sequence = titleInfo.Elements().FirstOrDefault(e => e.Name.LocalName == "sequence");

            if (series == null)
            {
                sequence?.Remove();
                return;
            }

            if (sequence == null)
            {
                sequence = new XElement(titleInfo.Name.Namespace + "sequence");
                titleInfo.Elements().LastOrDefault()?.AddAfterSelf(sequence);

                if (sequence.Parent == null)
                {
                    titleInfo.Add(sequence);
                }
            }

            Fb2InfoWriter.SetAttribute(sequence, "name", series);
            Fb2InfoWriter.SetAttribute(sequence, "number", number);
        }

        private static void SetAttribute(XElement element, String name, String value)
        {
            var attribute = element.Attributes().FirstOrDefault(a => a.Name.LocalName == name);

            if (value == null)
            {
                attribute?.Remove();
            }
            else if ((attribute == null) || (Fb2InfoWriter.Normalize(attribute.Value) != value))
            {
                element.SetAttributeValue(attribute?.Name ?? XName.Get(name), value);
            }
        }

        // Adds the new element after the last of the named child elements, or as the first child if there is none
        private static void AddAfterLast(XElement parent, XElement newElement, params String[] names)
        {
            var last = parent.Elements().LastOrDefault(e => names.Contains(e.Name.LocalName));

            if (last != null)
            {
                last.AddAfterSelf(newElement);
            }
            else
            {
                parent.AddFirst(newElement);
            }
        }

        // Collapses white space (as the reader does); empty text becomes null
        private static String Normalize(String text)
        {
            if (text == null)
            {
                return null;
            }

            var normalized = String.Join(' ', text.Split((Char[])null, StringSplitOptions.RemoveEmptyEntries));

            return (normalized.Length == 0) ? null : normalized;
        }
    }
}
