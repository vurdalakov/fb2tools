namespace Vurdalakov
{
    using System;
    using System.IO;

    internal class SiteInfo
    {
        private readonly TextWriter _outputWriter;

        public String BaseDirectoryPath { get; }

        public String BaseUrl { get; }

        public String OpdsBaseDirectoryPath { get; }

        public String OpdsBaseUrl { get; }

        public String ImageDirectoryPath { get; }

        public String ThumbnailDirectoryPath { get; }

        public String ImageBaseUrl { get; }

        public String ThumbnailBaseUrl { get; }

        public String BookImageUrl { get; }

        public String BookThumbnailUrl { get; }

        public String FolderImageUrl { get; }

        public String FolderThumbnailUrl { get; }
            
        public SiteInfo(String baseDirectoryPath, String baseUrl, TextWriter outputWriter)
        {
            this._outputWriter = outputWriter;

            this.BaseDirectoryPath = baseDirectoryPath;
            this.BaseUrl = baseUrl;

            this.OpdsBaseDirectoryPath = Path.Combine(this.BaseDirectoryPath, Helpers.OpdsDirectoryName);
            this.OpdsBaseUrl = Helpers.CombineUrls(this.BaseUrl, Helpers.OpdsFolderName);

            this.ImageDirectoryPath = Path.Combine(this.OpdsBaseDirectoryPath, Helpers.ImageDirectoryName);
            this.ThumbnailDirectoryPath = Path.Combine(this.OpdsBaseDirectoryPath, Helpers.ThumbnailDirectoryName);

            this.ImageBaseUrl = Helpers.CombineUrls(this.OpdsBaseUrl, Helpers.ImageDirectoryName);
            this.ThumbnailBaseUrl = Helpers.CombineUrls(this.OpdsBaseUrl, Helpers.ThumbnailDirectoryName);

            this.BookImageUrl = this.GetImageUrl(Helpers.BookFileName);
            this.BookThumbnailUrl = this.GetThumbnailUrl(Helpers.BookFileName);

            this.FolderImageUrl = this.GetImageUrl(Helpers.FolderFileName);
            this.FolderThumbnailUrl = this.GetThumbnailUrl(Helpers.FolderFileName);
        }

        public String GetUrlFromPath(String path) => Helpers.PathToUrl(path, this.BaseDirectoryPath, this.BaseUrl);

        public String GetOpdsPathFromPath(String path) => Path.Combine(this.OpdsBaseDirectoryPath, Path.GetRelativePath(this.BaseDirectoryPath, path));

        public String GetOpdsUrlFromPath(String path) => Helpers.PathToUrl(path, this.BaseDirectoryPath, this.OpdsBaseUrl);

        public String GetImageFilePath(String fileName) => Path.Combine(this.ImageDirectoryPath, fileName);

        public String GetThumbnailFilePath(String fileName) => Path.Combine(this.ThumbnailDirectoryPath, fileName);

        public String GetImageUrl(String fileName) => Helpers.CombineUrls(this.ImageBaseUrl, fileName);

        public String GetThumbnailUrl(String fileName) => Helpers.CombineUrls(this.ThumbnailBaseUrl, fileName);

        public void WriteLine(String message) => this._outputWriter?.WriteLine(message);
    }
}