namespace Vurdalakov
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    // The checks of "fb2zip test" for one .fb2 or .fb2.zip file
    public static class Fb2FileChecker
    {
        private const String Fb2Extension = ".fb2";
        private const String ZipExtension = ".fb2.zip";

        // Zip entry names are often in a legacy code page such as 866
        static Fb2FileChecker() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Returns what is wrong with the file; an empty list means that it passes all the tests:
        //   .fb2.zip: an entry that is not Deflate, or a non-ASCII name without the UTF-8 flag
        //   .fb2: there is no .fb2.zip next to it
        //   both: the file name does not follow the XML of the book, or the XML cannot be read
        // legacyEncoding is the code page of zip entry names that are not UTF-8 (default 866)
        public static List<String> Check(String filePath, Encoding legacyEncoding = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(filePath);

            var problems = new List<String>();
            var isZip = filePath.EndsWith(Fb2FileChecker.ZipExtension, StringComparison.OrdinalIgnoreCase);
            var canReadBook = true;

            if (isZip)
            {
                try
                {
                    var entries = ZipDirectory.Read(filePath, legacyEncoding ?? Encoding.GetEncoding(866));

                    foreach (var entry in entries.Where(e => !e.Supported))
                    {
                        problems.Add($"{entry.Name}: {entry.Describe()}");
                    }

                    // .NET cannot read entries that are not Deflate, so the book is not checked
                    canReadBook = !entries.Any(e => e.BadMethod);
                }
                catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException)
                {
                    problems.Add($"cannot read zip: {exception.Message}");
                    return problems;
                }
            }
            else if (!File.Exists(filePath + ".zip"))
            {
                problems.Add("no .fb2.zip");
            }

            if (canReadBook)
            {
                Fb2FileChecker.CheckName(filePath, isZip, problems);
            }

            return problems;
        }

        private static void CheckName(String filePath, Boolean isZip, List<String> problems)
        {
            Fb2Info info;
            try
            {
                info = Fb2InfoReader.ReadFile(filePath);
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or System.Xml.XmlException)
            {
                problems.Add($"cannot read: {exception.Message}");
                return;
            }

            var expectedBaseName = Fb2FileName.Build(info, out var problem);
            if (expectedBaseName == null)
            {
                problems.Add(problem);
                return;
            }

            var extension = isZip ? Fb2FileChecker.ZipExtension : Fb2FileChecker.Fb2Extension;
            var name = Path.GetFileName(filePath);
            var actualBaseName = name.Substring(0, name.Length - extension.Length);
            var safeExpectedBaseName = Fb2FileName.MakeFileSystemSafe(expectedBaseName);

            if (!Fb2FileName.AreSame(actualBaseName, safeExpectedBaseName))
            {
                problems.Add($"name does not match XML, expected: {safeExpectedBaseName}{extension}");
            }
        }
    }
}
