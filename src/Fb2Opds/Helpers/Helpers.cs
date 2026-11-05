namespace Vurdalakov
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text;

    using StbImageResizeSharp;
    using StbImageSharp;
    using StbImageWriteSharp;

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
        private const Int32 ThumbnailJpegQuality = 85;

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
            // PNG keeps the alpha channel, JPEG has none
            var isPng = Path.GetExtension(thumbnailFilePath).Equals(".png", StringComparison.OrdinalIgnoreCase);
            var channels = isPng ? 4 : 3;
            var readComponents = isPng ? StbImageSharp.ColorComponents.RedGreenBlueAlpha : StbImageSharp.ColorComponents.RedGreenBlue;
            var writeComponents = isPng ? StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha : StbImageWriteSharp.ColorComponents.RedGreenBlue;

            var image = ImageResult.FromMemory(imageBytes, readComponents);

            var width = image.Width;
            var height = image.Height;
            var data = image.Data;

            var scale = Math.Min((Double)Helpers.MaxThumbnailSize / width, (Double)Helpers.MaxThumbnailSize / height);

            if (scale < 1.0)
            {
                var newWidth = Math.Max(1, (Int32)(width * scale));
                var newHeight = Math.Max(1, (Int32)(height * scale));

                var resizedData = new Byte[newWidth * newHeight * channels];

                if (0 == StbImageResize.stbir_resize_uint8(data, width, height, width * channels, resizedData, newWidth, newHeight, newWidth * channels, channels))
                {
                    throw new InvalidOperationException("Failed to resize image.");
                }

                data = resizedData;
                width = newWidth;
                height = newHeight;
            }

            using (var stream = File.Create(thumbnailFilePath))
            {
                var writer = new ImageWriter();

                if (isPng)
                {
                    writer.WritePng(data, width, height, writeComponents, stream);
                }
                else
                {
                    writer.WriteJpg(data, width, height, writeComponents, stream, Helpers.ThumbnailJpegQuality);
                }
            }
        }
    }
}