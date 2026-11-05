namespace Vurdalakov
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine;
    using System.IO;
    using System.IO.Compression;
    using System.IO.Hashing;
    using System.Linq;
    using System.Text;

    internal static class Program
    {
        private static Int32 Main(String[] args)
        {
            var rootCommand = new RootCommand(
                "Finds and fixes zip files that are not plain \"Deflate + UTF-8 names\".\n\n"
                + "Problems reported by 'test' and fixed by 'repack':\n"
                + "  - entry compressed with anything but Deflate (Stored, Deflate64, BZip2, LZMA, PPMd, ...)\n"
                + "  - non-ASCII entry name without the UTF-8 flag\n"
                + "Problem reported by 'test' and fixed by 'pack':\n"
                + "  - .fb2 file without a matching .fb2.zip\n"
                + "Problem reported by 'test' only:\n"
                + "  - .fb2 / .fb2.zip file name that does not follow the book (title-info author, title, sequence):\n"
                + "    'Author - Title' or 'Author - [Series, volume] - Title'\n"
                + "    ('Author - ' is omitted if the book has no author; ' (пер. Translator)' is added if it has a translator)\n\n"
                + "Encrypted entries are reported but cannot be repacked.");

            rootCommand.Subcommands.Add(Program.CreateCommand("test", "Report problems, change nothing", true, true, false, true));
            rootCommand.Subcommands.Add(Program.CreateCommand("repack", "Repack zip files that have problems (original is replaced in place)", true, false, true, false));
            rootCommand.Subcommands.Add(Program.CreateCommand("pack", "Create <name>.fb2.zip for every <name>.fb2 that has none", false, true, true, false));

            return rootCommand.Parse(args).Invoke();
        }

        private static Command CreateCommand(String name, String description, Boolean processZipFiles, Boolean processFb2Files, Boolean fix, Boolean checkFb2Names)
        {
            var command = new Command(name, description);

            var folderArgument = new Argument<String>("folder")
            {
                Description = "Folder to scan",
            };
            command.Arguments.Add(folderArgument);

            var noRecursiveOption = new Option<Boolean>("--no-recursive")
            {
                Description = "Scan only the given folder, not its sub-folders (default: recursive)",
            };
            command.Options.Add(noRecursiveOption);

            Option<String> encodingOption = null;
            if (processZipFiles)
            {
                encodingOption = new Option<String>("--encoding")
                {
                    Description = "Code page (number or name) for entry names that are neither UTF-8 nor flagged as UTF-8; use 437 for Western DOS zips",
                    DefaultValueFactory = _ => "866",
                };
                command.Options.Add(encodingOption);
            }

            Option<Boolean> backupOption = null;
            if (processZipFiles && fix)
            {
                backupOption = new Option<Boolean>("--backup")
                {
                    Description = "Keep each original as <name>.zip.bak",
                };
                command.Options.Add(backupOption);
            }

            command.SetAction(parseResult => Program.Run(
                parseResult.GetValue(folderArgument),
                !parseResult.GetValue(noRecursiveOption),
                (encodingOption == null) ? null : parseResult.GetValue(encodingOption),
                (backupOption != null) && parseResult.GetValue(backupOption),
                processZipFiles,
                processFb2Files,
                fix,
                checkFb2Names));

            return command;
        }

        private static Int32 Run(String folder, Boolean recurse, String encodingName, Boolean backup, Boolean processZipFiles, Boolean processFb2Files, Boolean fix, Boolean checkFb2Names)
        {
            if (!Directory.Exists(folder))
            {
                Console.Error.WriteLine($"Folder not found: {folder}");
                return 2;
            }

            Console.OutputEncoding = Encoding.UTF8;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Encoding legacyEncoding = null;
            if (encodingName != null)
            {
                try
                {
                    legacyEncoding = Int32.TryParse(encodingName, out var codePage) ? Encoding.GetEncoding(codePage) : Encoding.GetEncoding(encodingName);
                }
                catch (ArgumentException exception)
                {
                    Console.Error.WriteLine($"Bad --encoding: {exception.Message}");
                    return 2;
                }
            }

            var searchOption = recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var failed = 0;

            if (processZipFiles)
            {
                failed += Program.ProcessZipFiles(folder, searchOption, fix, backup, legacyEncoding);
            }

            if (processFb2Files)
            {
                failed += Program.ProcessFb2Files(folder, searchOption, fix);
            }

            if (checkFb2Names)
            {
                Fb2NameCheck.Run(folder, searchOption);
            }

            if (failed > 0)
            {
                Console.WriteLine($"{failed} error(s).");
            }

            return (failed > 0) ? 3 : 0;
        }

        /// <summary>Reports (and optionally repacks) zip files with non-Deflate entries or non-UTF-8 names. Returns the number of errors.</summary>
        private static Int32 ProcessZipFiles(String folder, SearchOption searchOption, Boolean repack, Boolean backup, Encoding legacyEncoding)
        {
            var scanned = 0;
            var affected = 0;
            var repacked = 0;
            var failed = 0;

            var zipFilePaths = Directory.EnumerateFiles(folder, "*.zip", searchOption).OrderBy(f => f, StringComparer.OrdinalIgnoreCase);
            foreach (var zipFilePath in zipFilePaths)
            {
                scanned++;

                List<ZipEntryInfo> entries;
                try
                {
                    entries = ZipDirectory.Read(zipFilePath, legacyEncoding);
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"[ERROR] {zipFilePath}: {exception.Message}");
                    failed++;
                    continue;
                }

                var badEntries = entries.Where(e => !e.Supported).ToList();
                if (badEntries.Count == 0)
                {
                    continue;
                }

                affected++;

                Console.WriteLine(zipFilePath);
                foreach (var entry in badEntries)
                {
                    Console.WriteLine($"    {entry.Name}  [{entry.Describe()}]");
                }

                if (!repack)
                {
                    continue;
                }

                try
                {
                    if (Program.Repack(zipFilePath, entries, backup))
                    {
                        repacked++;
                    }
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"    [FAILED] {exception.Message}");
                    failed++;
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Scanned {scanned} zip file(s); {affected} with problems" + (repack ? $"; {repacked} repacked" : String.Empty) + ".");

            return failed;
        }

        /// <summary>Reports (and optionally packs) .fb2 files without .fb2.zip. Returns the number of errors.</summary>
        private static Int32 ProcessFb2Files(String folder, SearchOption searchOption, Boolean pack)
        {
            var orphans = 0;
            var packed = 0;
            var failed = 0;

            var fb2FilePaths = Directory.EnumerateFiles(folder, "*.fb2", searchOption).OrderBy(f => f, StringComparer.OrdinalIgnoreCase);
            foreach (var fb2FilePath in fb2FilePaths)
            {
                var zipFilePath = fb2FilePath + ".zip";
                if (File.Exists(zipFilePath))
                {
                    continue;
                }

                orphans++;
                Console.WriteLine($"{fb2FilePath}  [no .fb2.zip]");

                if (!pack)
                {
                    continue;
                }

                try
                {
                    Fb2ZipPacker.Pack(fb2FilePath, zipFilePath);
                    packed++;
                    Console.WriteLine($"    [PACKED] -> {Path.GetFileName(zipFilePath)}");
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"    [FAILED] {exception.Message}");
                    failed++;
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Found {orphans} .fb2 file(s) without .fb2.zip" + (pack ? $"; {packed} packed" : String.Empty) + ".");

            return failed;
        }

        private static Boolean Repack(String zipFilePath, List<ZipEntryInfo> entries, Boolean backup)
        {
            if (entries.Any(e => e.Encrypted))
            {
                Console.WriteLine("    [SKIPPED] contains encrypted entries - cannot repack without the password.");
                return false;
            }

            var tempFilePath = zipFilePath + ".repack.tmp";
            try
            {
                using (var source = SharpCompress.Archives.Zip.ZipArchive.OpenArchive(zipFilePath))
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                using (var destination = new ZipArchive(fileStream, ZipArchiveMode.Create, false, new UTF8Encoding(false)))
                {
                    var sourceEntries = source.Entries.ToList();
                    if (sourceEntries.Count != entries.Count)
                    {
                        throw new InvalidDataException("Entry count mismatch between readers.");
                    }

                    for (var i = 0; i < sourceEntries.Count; i++)
                    {
                        var sourceEntry = sourceEntries[i];
                        var info = entries[i];

                        var destinationEntry = destination.CreateEntry(info.Name, CompressionLevel.Optimal);
                        if ((sourceEntry.LastModifiedTime is DateTime lastWriteTime) && (lastWriteTime.Year >= 1980) && (lastWriteTime.Year <= 2107))
                        {
                            destinationEntry.LastWriteTime = lastWriteTime;
                        }

                        if (info.IsDirectory)
                        {
                            continue;
                        }

                        using (var input = sourceEntry.OpenEntryStream())
                        using (var output = destinationEntry.Open())
                        {
                            input.CopyTo(output);
                        }
                    }
                }

                Program.Verify(tempFilePath, entries);

                if (backup)
                {
                    File.Replace(tempFilePath, zipFilePath, zipFilePath + ".bak");
                }
                else
                {
                    File.Move(tempFilePath, zipFilePath, true);
                }

                Console.WriteLine("    [REPACKED] -> Deflate, UTF-8 names" + (backup ? " (backup: .bak)" : String.Empty));
                return true;
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        /// <summary>Re-reads the new archive and checks method, name flag, names, sizes and CRCs against the original.</summary>
        private static void Verify(String zipFilePath, List<ZipEntryInfo> expected)
        {
            var actual = ZipDirectory.Read(zipFilePath, Encoding.UTF8);
            if (actual.Count != expected.Count)
            {
                throw new InvalidDataException("Verification failed: entry count differs.");
            }

            using (var archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Read, Encoding.UTF8))
            {
                for (var i = 0; i < expected.Count; i++)
                {
                    var expectedEntry = expected[i];
                    var actualEntry = actual[i];

                    if (!actualEntry.Supported)
                    {
                        throw new InvalidDataException($"Verification failed: {expectedEntry.Name} is still not Deflate/UTF-8.");
                    }

                    if (actualEntry.Name != expectedEntry.Name)
                    {
                        throw new InvalidDataException($"Verification failed: name mismatch at {expectedEntry.Name}.");
                    }

                    if (expectedEntry.IsDirectory)
                    {
                        continue;
                    }

                    var crc = new Crc32();
                    using (var stream = archive.Entries[i].Open())
                    {
                        crc.Append(stream);
                    }

                    if ((crc.GetCurrentHashAsUInt32() != expectedEntry.Crc32) || ((UInt64)archive.Entries[i].Length != expectedEntry.Size))
                    {
                        throw new InvalidDataException($"Verification failed: content mismatch for {expectedEntry.Name}.");
                    }
                }
            }
        }
    }
}
