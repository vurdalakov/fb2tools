namespace Vurdalakov
{
    using System;
    using System.IO;

    internal static class BookInfoReader
    {
        public static BookInfo Read(FileInfo fileInfo)
        {
            var bookInfo = new BookInfo(fileInfo);

            Fb2Info fb2Info;
            using (var fileStream = fileInfo.OpenRead())
            {
                fb2Info = Fb2InfoReader.Read(fileStream, true);
            }

            bookInfo.Author = fb2Info.Author;
            bookInfo.Translator = fb2Info.Translator;
            bookInfo.Title = fb2Info.Title;
            bookInfo.Language = fb2Info.Language;
            bookInfo.Annotation = fb2Info.Annotation;

            bookInfo.Id = Guid.TryParse(fb2Info.DocumentId, out var guid) ? guid : Helpers.GuidFromString($"{bookInfo.Author}|{bookInfo.Title}");

            BookInfoReader.SaveCoverImages(fb2Info, bookInfo);

            bookInfo.ExtractBookInfo(fileInfo);

            return bookInfo;
        }

        private static void SaveCoverImages(Fb2Info fb2Info, BookInfo bookInfo)
        {
            if (fb2Info.CoverError != null)
            {
                OpdsGenerator.SiteInfo.WriteLine($"[warn]  Failed to extract cover image for '{bookInfo}': {fb2Info.CoverError}");
                return;
            }

            if (fb2Info.CoverImage == null)
            {
                return;
            }

            var fileExtension = fb2Info.CoverContentType == "image/png" ? "png" : "jpg";
            var fileName = $"{bookInfo.Id}.{fileExtension}";

            bookInfo.SaveBookImages(fb2Info.CoverImage, fileName);
        }
    }
}
