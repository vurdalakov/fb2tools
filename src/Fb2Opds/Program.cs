namespace Vurdalakov
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Help;
    using System.CommandLine.Invocation;
    using System.IO;

    internal static class Program
    {
        public static void Main(String[] args)
        {
            var assemblyInfo = AssemblyInfo.Read();

            var rootCommand = new RootCommand(assemblyInfo.Description);

            var pathOption = new Option<String>("--path")
            {
                Description = "Path to the root directory of the library, e.g. \"C:\\MyLibrary\"",
                Required = true,
            };
            rootCommand.Options.Add(pathOption);

            var urlOption = new Option<String>("--url")
            {
                Description = "Base URL of the library, e.g. \"https://vurdalakov.net/books\"",
                Required = true,
            };
            rootCommand.Options.Add(urlOption);

            var nameOption = new Option<String>("--name")
            {
                Description = "Name of the library, e.g. \"My library\".\nIf missing, the library directory name is used.",
                Required = false,
            };
            rootCommand.Options.Add(nameOption);

            var webhookTokenOption = new Option<String>("--webhook-token")
            {
                Description = "Shared secret for an optional rebuild webhook.\nIf set, writes \"opds/rebuild.php\" alongside \"opds/opds.xml\": an authenticated HTTP endpoint that re-runs this exact command in the background. Omit to skip generating it.\nMust be supplied on every run that should keep the webhook — a run without it removes rebuild.php.",
                Required = false,
            };
            rootCommand.Options.Add(webhookTokenOption);

            rootCommand.SetAction(parseResult =>
            {
                var baseDirectoryPath = parseResult.GetValue(pathOption);
                var baseUrl = parseResult.GetValue(urlOption);
                var catalogName = parseResult.GetValue(nameOption) ?? Path.GetFileName(baseDirectoryPath);
                var webhookToken = parseResult.GetValue(webhookTokenOption);

                var outputWriter = parseResult.InvocationConfiguration.Output;

                if (!Directory.Exists(baseDirectoryPath))
                {
                    outputWriter.WriteLine($"Error: library directory not found: \"{baseDirectoryPath}\"");
                    Environment.Exit(1);
                    return;
                }

                using var instanceLock = Program.TryAcquireInstanceLock(baseDirectoryPath);
                if (instanceLock == null)
                {
                    outputWriter.WriteLine($"Error: another Fb2Opds instance is already running for \"{baseDirectoryPath}\".");
                    Environment.Exit(1);
                    return;
                }

                OpdsGenerator.Run(baseDirectoryPath, baseUrl, catalogName, webhookToken, outputWriter);
            });

            var parseResult = rootCommand.Parse(args);

            if ((parseResult.Action is HelpAction) || (parseResult.Action is ParseErrorAction))
            {
                var outputWriter = parseResult.InvocationConfiguration.Output;

                outputWriter.WriteLine("---------------------------------------------------");
                outputWriter.WriteLine($"{assemblyInfo.Product} v{assemblyInfo.Version}");
                outputWriter.WriteLine($"{assemblyInfo.Copyright}");
                outputWriter.WriteLine("https://github.com/vurdalakov/opds-generator");
                outputWriter.WriteLine("---------------------------------------------------");
                outputWriter.WriteLine();

                var baseAction = parseResult.Action as SynchronousCommandLineAction;
                baseAction?.Invoke(parseResult);

                outputWriter.WriteLine();
                outputWriter.WriteLine("Example:");
                outputWriter.WriteLine($"    {assemblyInfo.FileName} --path \"C:\\MyLibrary\" --url \"https://vurdalakov.net/books\" --name \"My library\"");
                outputWriter.WriteLine();
                outputWriter.WriteLine("Example with a rebuild webhook:");
                outputWriter.WriteLine($"    {assemblyInfo.FileName} --path \"C:\\MyLibrary\" --url \"https://vurdalakov.net/books\" --name \"My library\" --webhook-token \"a-long-random-secret\"");
            }
            else
            {
                parseResult.Invoke();
            }
        }

        // Scoped per library path (rather than one global lock) so two different libraries can be
        // generated concurrently without contention. Released automatically on process exit/crash,
        // same guarantee an OS-level file lock always gives.
        private static FileStream TryAcquireInstanceLock(String baseDirectoryPath)
        {
            var lockFilePath = Path.Combine(Path.GetTempPath(), $"opds-generator-{Helpers.GuidFromString(baseDirectoryPath):N}.lock");

            try
            {
                return new FileStream(lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return null;
            }
        }
    }
}