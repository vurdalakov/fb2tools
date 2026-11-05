namespace Vurdalakov
{
    using System;
    using System.IO;

    [System.Diagnostics.DebuggerDisplay("{ToString()}")]
    internal class BookInfo
    {
        public Guid Id { get; set; }

        public String Title { get; set; }

        public String Author { get; set; }

        public String Translator { get; set; }

        public String Language { get; set; }

        public DateTime Updated { get; set; }

        public String Annotation { get; set; }

        public String FileUrl { get; set; }

        public String FileContentType { get; set; }

        public String ImageUrl { get; set; }

        public String ThumbnailUrl { get; set; }

        public BookInfo(FileInfo fileInfo)
        {
            this.FileUrl = OpdsGenerator.SiteInfo.GetUrlFromPath(fileInfo.FullName);
            this.FileContentType = "application/fb2";
            this.Updated = fileInfo.LastWriteTimeUtc;

            this.ImageUrl = OpdsGenerator.SiteInfo.BookImageUrl;
            this.ThumbnailUrl = OpdsGenerator.SiteInfo.BookThumbnailUrl;
        }

        public override String ToString()
        {
            var text = $"{this.Author} - {this.Title}";
            return String.IsNullOrEmpty(this.Translator) ? text : $"{text} (пер. {this.Translator})";
        }
    }
}