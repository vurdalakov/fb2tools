namespace Vurdalakov
{
    using System;
    using System.IO;
    using System.Xml.Linq;

    internal static class Fb2InfoReader
    {
        private static readonly XNamespace _xlinkNamespace = "http://www.w3.org/1999/xlink";

        public static BookInfo Read(FileInfo fileInfo)
        {
            using var fileStream = fileInfo.OpenRead();

            var bookInfo = new BookInfo(fileInfo);

            var encoding = fileStream.GetXmlEncoding();

            using var streamReader = new StreamReader(fileStream, encoding);

            var document = XDocument.Load(streamReader);
            var titleInfo = document.GetElement("title-info");

            bookInfo.Author = titleInfo.ReadFirstLastName("author");
            bookInfo.Translator = titleInfo.ReadFirstLastName("translator");

            bookInfo.Title = titleInfo.GetValue("book-title");
            bookInfo.Language = titleInfo.GetValue("lang");

            bookInfo.Annotation = titleInfo.GetInnerXml("annotation");

            bookInfo.Id = document.ReadDocumentId(bookInfo);

            document.SaveCoverImages(bookInfo);

            bookInfo.ExtractBookInfo(fileInfo);

            return bookInfo;
        }

        private static String ReadFirstLastName(this XContainer titleInfo, String nodeName)
        {
            var translatorNode = titleInfo.GetElement(nodeName);

            if (translatorNode != null)
            {
                var firstName = translatorNode.GetValue("first-name");
                var lastName = translatorNode.GetValue("last-name");
                return $"{firstName} {lastName}";
            }

            return null;
        }

        private static Guid ReadDocumentId(this XDocument document, BookInfo bookInfo)
        {
            var id = document.GetValue("document-info", "id");

            return Guid.TryParse(id, out var guid) ? guid : Helpers.GuidFromString($"{bookInfo.Author}|{bookInfo.Title}");
        }

        private static void SaveCoverImages(this XDocument document, BookInfo bookInfo)
        {
            var coverImage = document.GetElement("coverpage", "image");
            if (null == coverImage)
            {
                return;
            }

            var href = coverImage.Attribute(Fb2InfoReader._xlinkNamespace + "href")?.Value;
            if (null == href)
            {
                return;
            }

            var id = href.TrimStart('#');

            var binaryNode = document.GetElementWithAtribute("binary", "id", id);
            if (null == binaryNode)
            {
                return;
            }

            var contentType = binaryNode.GetAttributeValue("content-type", "image/jpeg");
            var fileExtension = contentType == "image/png" ? "png" : "jpg";

            try
            {
                var imageBytes = Convert.FromBase64String(binaryNode.Value.Trim());
                var fileName = $"{bookInfo.Id}.{fileExtension}";

                bookInfo.SaveBookImages(imageBytes, fileName);
            }
            catch (Exception exception)
            {
                OpdsGenerator.SiteInfo.WriteLine($"[warn]  Failed to extract cover image for '{bookInfo}': {exception.Message}");
            }
        }
    }
}