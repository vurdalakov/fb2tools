namespace Vurdalakov
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text;

    using SixLabors.ImageSharp;
    using SixLabors.ImageSharp.Processing;

    internal static class Helpers
    {
        public const String OpdsDirectoryName = ".opds"; // path
        public const String OpdsFolderName = "opds"; // url
        public const String ImageDirectoryName = ".images";
        public const String ThumbnailDirectoryName = ".thumbnails";

        public const String BookFileName = ".book.png";
        public const String FolderFileName = ".folder.png";

        public static Guid GuidFromString(String text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var hash = MD5.HashData(bytes);
            return new Guid(hash);
        }

        public static Byte[] ReadFromResource(String resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using var stream = assembly.GetManifestResourceStream($"Vurdalakov.Resources.{resourceName}") ?? throw new InvalidOperationException($"Resource not found: {resourceName}");

            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }

        public static String CombineUrls(String url1, String url2)
        {
            if (url1.EndsWith('/'))
            {
                url1 = url1.Substring(0, url1.Length - 1);
            }

            if (url2.StartsWith('/'))
            {
                url2 = url2.Substring(1);
            }

            var url = $"{url1}/{url2}";

            return Helpers.FixUrl(url);
        }

        public static String FixUrl(String url) => url.Replace('\\', '/');

        public static String PathToUrl(String path, String baseDir, String baseUrl)
        {
            var relativePath = Path.GetRelativePath(baseDir, path);

            var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            for (var i = 0; i < segments.Length; i++)
            {
                segments[i] = Uri.EscapeDataString(segments[i]);
            }

            var encodedPath = String.Join("/", segments);

            var url = baseUrl.TrimEnd('/') + "/" + encodedPath;
            return url;
        }

        public static void ExtractBookInfo(this BookInfo bookInfo, FileInfo fileInfo)
        {
            var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name).Trim();

            var bracket = fileName.IndexOf('(');
            if ((bracket > 0) && fileName.EndsWith(')'))
            {
                fileName = fileName.Substring(0, bracket).TrimEnd();
            }

            var parts = fileName.Split(" - ", StringSplitOptions.None);

            if (parts.Length >= 2)
            {
                if (String.IsNullOrWhiteSpace(bookInfo.Author))
                {
                    bookInfo.Author = parts[0].Trim();
                }

                if (String.IsNullOrWhiteSpace(bookInfo.Title))
                {
                    bookInfo.Title = parts[1].Trim();
                }
            }
            else if (String.IsNullOrWhiteSpace(bookInfo.Title))
            {
                bookInfo.Title = fileName;
            }
        }

        private const Int32 MaxThumbnailSize = 200;

        public static void SaveBookImages(this BookInfo bookInfo, Byte[] imageBytes, String fileName)
        {
            var imageFilePath = OpdsGenerator.SiteInfo.GetImageFilePath(fileName);
            var thumbnailFilePath = OpdsGenerator.SiteInfo.GetThumbnailFilePath(fileName);

            try
            {
                File.WriteAllBytes(imageFilePath, imageBytes);
                bookInfo.ImageUrl = OpdsGenerator.SiteInfo.GetImageUrl(fileName);

                Helpers.SaveThumbnailImage(imageBytes, thumbnailFilePath);
                bookInfo.ThumbnailUrl = OpdsGenerator.SiteInfo.GetThumbnailUrl(fileName);
            }
            catch (Exception exception)
            {
                OpdsGenerator.SiteInfo.WriteLine($"[warn]  Failed to save cover image '{fileName}' for '{bookInfo}': {exception.Message}");
            }
        }

        public static void SaveThumbnailImage(Byte[] imageBytes, String thumbnailFilePath)
        {
            using var image = Image.Load(imageBytes);

            if ((image.Width <= MaxThumbnailSize) && (image.Height <= MaxThumbnailSize))
            {
                image.Save(thumbnailFilePath);
                return;
            }

            var scale = Math.Min((Double)MaxThumbnailSize / image.Width, (Double)MaxThumbnailSize / image.Height);

            if (scale >= 1.0)
            {
                image.Save(thumbnailFilePath);
                return;
            }

            var newWidth = (Int32)(image.Width * scale);
            var newHeight = (Int32)(image.Height * scale);

            image.Mutate(x => x.Resize(newWidth, newHeight));
            image.Save(thumbnailFilePath);
        }
    }
}