namespace Vurdalakov
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Text;

    public static class Fb2ZipPacker
    {
        // Creates the zip file holding the .fb2 file as its only entry: Deflate, UTF-8 name.
        // The zip is checked (layout and content) before it gets its final name; an existing zip file is never overwritten.
        public static void Pack(String filePath, String zipFilePath)
        {
            ArgumentException.ThrowIfNullOrEmpty(filePath);
            ArgumentException.ThrowIfNullOrEmpty(zipFilePath);

            var tempFilePath = zipFilePath + ".pack.tmp";
            try
            {
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, false, new UTF8Encoding(false)))
                {
                    var entry = archive.CreateEntry(Path.GetFileName(filePath), CompressionLevel.Optimal);

                    var lastWriteTime = File.GetLastWriteTime(filePath);
                    if ((lastWriteTime.Year >= 1980) && (lastWriteTime.Year <= 2107))
                    {
                        entry.LastWriteTime = lastWriteTime;
                    }

                    using (var input = File.OpenRead(filePath))
                    using (var output = entry.Open())
                    {
                        input.CopyTo(output);
                    }
                }

                Fb2ZipPacker.Verify(filePath, tempFilePath);

                File.Move(tempFilePath, zipFilePath, false);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        // One Deflate entry with a UTF-8 name (if needed), and exactly the content of the file
        private static void Verify(String filePath, String zipFilePath)
        {
            var entries = ZipDirectory.Read(zipFilePath, Encoding.UTF8);
            if ((entries.Count != 1) || !entries[0].Supported)
            {
                throw new InvalidDataException("Verification failed: unexpected archive layout.");
            }

            using (var archive = ZipFile.OpenRead(zipFilePath))
            using (var zipped = archive.Entries[0].Open())
            using (var original = File.OpenRead(filePath))
            {
                if (!Fb2ZipPacker.SameContent(original, zipped))
                {
                    throw new InvalidDataException("Verification failed: content mismatch.");
                }
            }
        }

        private static Boolean SameContent(Stream first, Stream second)
        {
            var firstBuffer = new Byte[81920];
            var secondBuffer = new Byte[81920];

            while (true)
            {
                var firstCount = first.ReadAtLeast(firstBuffer, firstBuffer.Length, false);
                var secondCount = second.ReadAtLeast(secondBuffer, secondBuffer.Length, false);

                if ((firstCount != secondCount) || !firstBuffer.AsSpan(0, firstCount).SequenceEqual(secondBuffer.AsSpan(0, secondCount)))
                {
                    return false;
                }

                if (firstCount == 0)
                {
                    return true;
                }
            }
        }
    }
}
