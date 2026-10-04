using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using McMaster.Extensions.CommandLineUtils;
using NuGet.Common;

namespace NupkgWrench
{
    internal static class ExtractCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("extract", cmd =>
            {
                cmd.UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.Throw;
                Run(cmd, log);
            });
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Extract a nupkg to a folder.";
            cmd.HelpOption(Constants.HelpOption);
            var idFilter = cmd.Option(Constants.IdFilterTemplate, Constants.IdFilterDesc, CommandOptionType.SingleValue);
            var versionFilter = cmd.Option(Constants.VersionFilterTemplate, Constants.VersionFilterDesc, CommandOptionType.SingleValue);
            var excludeSymbolsFilter = cmd.Option(Constants.ExcludeSymbolsTemplate, Constants.ExcludeSymbolsDesc, CommandOptionType.NoValue);
            var highestVersionFilter = cmd.Option(Constants.HighestVersionFilterTemplate, Constants.HighestVersionFilterDesc, CommandOptionType.NoValue);

            var output = cmd.Option("-o|--output", "Output folder, all nupkg files will be placed in the root of this folder.", CommandOptionType.SingleValue);

            var argRoot = cmd.Argument(
                "[root]",
                Constants.SinglePackageRootDesc,
                multipleValues: true);

            var required = new List<CommandOption>()
            {
                output
            };

            cmd.OnExecute(() =>
            {
                // Validate parameters
                foreach (var requiredOption in required)
                {
                    if (!requiredOption.HasValue())
                    {
                        throw new ArgumentException($"Missing required parameter --{requiredOption.LongName}.");
                    }
                }

                var inputs = argRoot.Values.Select(v => v!).ToList();

                if (inputs.Count < 1)
                {
                    inputs.Add(Directory.GetCurrentDirectory());
                }

                var nupkgPath = Util.GetSinglePackageWithFilter(idFilter, versionFilter, excludeSymbolsFilter, highestVersionFilter, inputs.ToArray());

                var outputRoot = GetOutputRoot(output.Value()!);

                using (var stream = File.OpenRead(nupkgPath))
                using (var zip = new ZipArchive(stream))
                {
                    log.LogMinimal($"Extracting {nupkgPath} -> {output.Value()}");

                    // Validate all entries before writing anything to disk.
                    var files = new List<(ZipArchiveEntry Entry, string Path)>();

                    foreach (var entry in zip.Entries)
                    {
                        files.Add((entry, GetEntryPath(outputRoot, entry)));
                    }

                    Directory.CreateDirectory(outputRoot);

                    foreach (var (entry, path) in files)
                    {
                        var dir = Path.GetDirectoryName(path);
                        Directory.CreateDirectory(dir!);

                        log.LogInformation($"writing {path}");

                        using (var entryStream = entry.Open())
                        using (var outputStream = File.Create(path))
                        {
                            entryStream.CopyTo(outputStream);
                        }
                    }
                }

                return 0;
            });
        }

        /// <summary>
        /// Full path of the output folder, ending with a directory separator.
        /// </summary>
        private static string GetOutputRoot(string output)
        {
            var root = Path.GetFullPath(output);

            if (!Path.EndsInDirectorySeparator(root))
            {
                root += Path.DirectorySeparatorChar;
            }

            return root;
        }

        /// <summary>
        /// Full path to extract the entry to. Throws if the path is not under the output folder.
        /// </summary>
        private static string GetEntryPath(string outputRoot, ZipArchiveEntry entry)
        {
            var path = Path.GetFullPath(Path.Combine(outputRoot, entry.FullName.Replace('/', Path.DirectorySeparatorChar)));
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            if (path.Length <= outputRoot.Length || !path.StartsWith(outputRoot, comparison))
            {
                throw new InvalidDataException($"Package entry '{entry.FullName}' resolves to a path outside of the output folder '{outputRoot}'. No files were extracted.");
            }

            return path;
        }
    }
}