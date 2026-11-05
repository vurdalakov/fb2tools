namespace Vurdalakov
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    internal sealed class OpdsGenerator
    {
        // static

        public static SiteInfo SiteInfo { get; private set; }

        public static void Run(String baseDirectoryPath, String baseUrl, String catalogName, String webhookToken, TextWriter outputWriter)
        {
            var siteInfo = new SiteInfo(baseDirectoryPath, baseUrl, outputWriter);

            if (Directory.Exists(siteInfo.OpdsBaseDirectoryPath))
            {
                Directory.Delete(siteInfo.OpdsBaseDirectoryPath, true);
            }

            Directory.CreateDirectory(siteInfo.OpdsBaseDirectoryPath);
            Directory.CreateDirectory(siteInfo.ImageDirectoryPath);
            Directory.CreateDirectory(siteInfo.ThumbnailDirectoryPath);

            File.WriteAllBytes(siteInfo.GetImageFilePath(Helpers.BookFileName), Helpers.ReadFromResource("book1.png"));
            File.WriteAllBytes(siteInfo.GetImageFilePath(Helpers.FolderFileName), Helpers.ReadFromResource("folder1.png"));
            File.WriteAllBytes(siteInfo.GetThumbnailFilePath(Helpers.BookFileName), Helpers.ReadFromResource("book2.png"));
            File.WriteAllBytes(siteInfo.GetThumbnailFilePath(Helpers.FolderFileName), Helpers.ReadFromResource("folder2.png"));

            OpdsGenerator.SiteInfo = siteInfo;

            OpdsGenerator.ProcessDirectory(siteInfo.BaseDirectoryPath, catalogName);

            if (!String.IsNullOrWhiteSpace(webhookToken))
            {
                WebhookGenerator.Write(siteInfo, baseDirectoryPath, baseUrl, catalogName, webhookToken);
                siteInfo.WriteLine($" [info]  Wrote rebuild webhook: {siteInfo.OpdsBaseUrl}/rebuild.php");
            }
        }

        private static void ProcessDirectory(String directoryPath, String directoryDisplayName)
        {
            OpdsGenerator.SiteInfo.WriteLine($"-------- '{directoryPath}'");

            var directoryId = Helpers.GuidFromString(directoryDisplayName);

            var opdsGenerator = new OpdsGenerator(directoryId, directoryDisplayName);

            var directoryInfo = new DirectoryInfo(directoryPath);

            var subdirectoryInfos = directoryInfo.EnumerateDirectories()
                .Where(subdirectoryInfo => !SkipDirectory(subdirectoryInfo))
                .ToList();

            foreach (var subdirectoryInfo in subdirectoryInfos)
            {
                var folderUrl = OpdsGenerator.SiteInfo.GetOpdsUrlFromPath(subdirectoryInfo.FullName);
                var folderName = subdirectoryInfo.Name;

                opdsGenerator.AddFolderEntry(folderUrl, folderName);

                OpdsGenerator.SiteInfo.WriteLine($"[folder] '{folderName}'");
            }

            foreach (var fileInfo in directoryInfo.EnumerateFiles("*.fb2"))
            {
                try
                {
                    var bookInfo = Fb2InfoReader.Read(fileInfo);

                    opdsGenerator.AddFileEntry(bookInfo);

                    OpdsGenerator.SiteInfo.WriteLine($" [file]  '{bookInfo}'");
                }
                catch (Exception exception)
                {
                    OpdsGenerator.SiteInfo.WriteLine($" [error] Skipped '{fileInfo.FullName}': {exception.Message}");
                }
            }

            var opdsDirectoryPath = OpdsGenerator.SiteInfo.GetOpdsPathFromPath(directoryPath);
            opdsGenerator.Save(opdsDirectoryPath);

            foreach (var subdirectoryInfo in subdirectoryInfos)
            {
                ProcessDirectory(subdirectoryInfo.FullName, subdirectoryInfo.Name);
            }

            Boolean SkipDirectory(DirectoryInfo directoryInfo) => directoryInfo.Name.StartsWith('.') || !HasBooks(directoryInfo);
        }

        private static Boolean HasBooks(DirectoryInfo directoryInfo)
        {
            foreach (var fileSystemInfo in directoryInfo.EnumerateFileSystemInfos())
            {
                if (fileSystemInfo is FileInfo fileInfo)
                {
                    if (fileInfo.Extension.Equals(".fb2", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    continue;
                }

                if ((fileSystemInfo is DirectoryInfo subdirectoryInfo) && !subdirectoryInfo.Name.StartsWith('.') && HasBooks(subdirectoryInfo))
                {
                    return true;
                }
            }

            return false;
        }

        // instance

        private readonly XDocument _document;
        private readonly XNamespace _namespace = "http://www.w3.org/2005/Atom";

        public OpdsGenerator(Guid id, String title)
        {
            XNamespace opds = "http://opds-spec.org/2010/catalog";

            var root = new XElement(
                this._namespace + "feed",
                new XAttribute(XNamespace.Xmlns + "opds", opds),
                new XElement(this._namespace + "id", "urn:uuid:" + id),
                new XElement(this._namespace + "title", title),
                new XElement(this._namespace + "updated", DateTime.UtcNow.ToString("o")));

            this._document = new XDocument(root);
        }

        public void Save(String opdsDirectoryPath)
        {
            Directory.CreateDirectory(opdsDirectoryPath);

            var htaccessFilePath = Path.Combine(opdsDirectoryPath, ".htaccess");
            File.WriteAllText(htaccessFilePath, "DirectoryIndex opds.xml");

            var catalogFilePath = Path.Combine(opdsDirectoryPath, "opds.xml");
            this._document.Save(catalogFilePath);
        }

        public void AddFolderEntry(String folderUrl, String folderName)
        {
            var entry = new XElement(
                this._namespace + "entry",
                new XElement(this._namespace + "id", "urn:uuid:" + Helpers.GuidFromString(folderUrl)),
                new XElement(this._namespace + "title", folderName),
                new XElement(
                    this._namespace + "link",
                    new XAttribute("rel", "http://opds-spec.org/image"),
                    new XAttribute("href", OpdsGenerator.SiteInfo.FolderImageUrl),
                    new XAttribute("type", "image/png")),
                new XElement(
                    this._namespace + "link",
                    new XAttribute("rel", "http://opds-spec.org/thumbnail"),
                    new XAttribute("href", OpdsGenerator.SiteInfo.FolderThumbnailUrl),
                    new XAttribute("type", "image/png")),
                new XElement(
                    this._namespace + "link",
                    new XAttribute("rel", "subsection"),
                    new XAttribute("href", folderUrl),
                    new XAttribute("type", "application/atom+xml;profile=opds-catalog")));

            this._document.Root.Add(entry);
        }

        public void AddFileEntry(BookInfo bookInfo)
        {
            var entry = new XElement(
                this._namespace + "entry",
                new XElement(this._namespace + "id", "urn:uuid:" + bookInfo.Id),
                new XElement(this._namespace + "author", new XElement(this._namespace + "name", bookInfo.Author)),
                new XElement(this._namespace + "title", bookInfo.Title),
                new XElement(this._namespace + "updated", bookInfo.Updated.ToString("o")),
                new XElement(this._namespace + "content", bookInfo.Annotation),
                new XElement(
                    this._namespace + "link",
                    new XAttribute("rel", "http://opds-spec.org/image"),
                    new XAttribute("href", bookInfo.ImageUrl),
                    new XAttribute("type", GetContentType(bookInfo.ImageUrl))),
                new XElement(
                    this._namespace + "link",
                    new XAttribute("rel", "http://opds-spec.org/thumbnail"),
                    new XAttribute("href", bookInfo.ThumbnailUrl),
                    new XAttribute("type", GetContentType(bookInfo.ImageUrl))),
                new XElement(
                    this._namespace + "link",
                    new XAttribute("rel", "http://opds-spec.org/acquisition"),
                    new XAttribute("href", bookInfo.FileUrl),
                    new XAttribute("type", bookInfo.FileContentType)));

            this._document.Root.Add(entry);

            String GetContentType(String filePath) => filePath.EndsWith(".png") ? "image/png" : "image/jpeg";
        }
    }
}