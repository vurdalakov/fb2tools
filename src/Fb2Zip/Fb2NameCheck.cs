namespace Vurdalakov
{
    using System;
    using System.IO;
    using System.Linq;

    /// <summary>Checks that FB2 file names follow what is inside the XML.</summary>
    internal static class Fb2NameCheck
    {
        private const String Fb2Extension = ".fb2";
        private const String ZipExtension = ".fb2.zip";

        public static void Run(String folder, SearchOption searchOption)
        {
            var checkedCount = 0;
            var problemCount = 0;

            var filePaths = Directory.EnumerateFiles(folder, "*", searchOption)
                .Where(p => p.EndsWith(Fb2NameCheck.Fb2Extension, StringComparison.OrdinalIgnoreCase) || p.EndsWith(Fb2NameCheck.ZipExtension, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);

            foreach (var filePath in filePaths)
            {
                checkedCount++;

                var isZip = filePath.EndsWith(Fb2NameCheck.ZipExtension, StringComparison.OrdinalIgnoreCase);
                var extension = isZip ? Fb2NameCheck.ZipExtension : Fb2NameCheck.Fb2Extension;
                var actualName = Path.GetFileName(filePath);
                var actualBaseName = actualName.Substring(0, actualName.Length - extension.Length);

                String problem = null;
                String expectedName = null;

                try
                {
                    var info = isZip ? Fb2NameCheck.ReadFromZip(filePath, out problem) : Fb2NameCheck.ReadFromFile(filePath);

                    if ((info != null) && (problem == null))
                    {
                        var expectedBaseName = Fb2FileName.Build(info, out problem);

                        if (expectedBaseName != null)
                        {
                            expectedBaseName = Fb2FileName.MakeFileSystemSafe(expectedBaseName);
                            expectedName = expectedBaseName + extension;

                            if (!Fb2FileName.AreSame(actualBaseName, expectedBaseName))
                            {
                                problem = "name does not match XML";
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    problem = $"cannot read: {exception.Message}";
                }

                if (problem == null)
                {
                    continue;
                }

                problemCount++;

                Console.WriteLine($"{filePath}  [{problem}]");

                if (expectedName != null)
                {
                    Console.WriteLine($"    expected: {expectedName}");
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Checked {checkedCount} FB2 file name(s); {problemCount} with problems.");
        }

        private static Fb2Info ReadFromFile(String filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                return Fb2InfoReader.Read(stream);
            }
        }

        // Uses SharpCompress so that entries with any compression method can be read
        private static Fb2Info ReadFromZip(String zipFilePath, out String problem)
        {
            problem = null;

            using (var archive = SharpCompress.Archives.Zip.ZipArchive.OpenArchive(zipFilePath))
            {
                var entries = archive.Entries.Where(e => !e.IsDirectory && (e.Key != null) && e.Key.EndsWith(Fb2NameCheck.Fb2Extension, StringComparison.OrdinalIgnoreCase)).ToList();

                if (entries.Count != 1)
                {
                    problem = (entries.Count == 0) ? "no .fb2 entry in zip" : $"{entries.Count} .fb2 entries in zip";
                    return null;
                }

                using (var stream = entries[0].OpenEntryStream())
                {
                    return Fb2InfoReader.Read(stream);
                }
            }
        }
    }
}
