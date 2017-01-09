using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;

namespace NupkgWrench
{
    internal static class ExtractCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("extract", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
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

                var inputs = argRoot.Values;

                if (inputs.Count < 1)
                {
                    inputs.Add(Directory.GetCurrentDirectory());
                }

                var nupkgPath = Util.GetSinglePackageWithFilter(idFilter, versionFilter, excludeSymbolsFilter, highestVersionFilter, inputs.ToArray());

                Directory.CreateDirectory(output.Value());

                using (var stream = File.OpenRead(nupkgPath))
                using (var zip = new ZipArchive(stream))
                {
                    log.LogMinimal($"Extracting {nupkgPath} -> {output.Value()}");

                    foreach (var entry in zip.Entries)
                    {
                        var path = Path.Combine(output.Value(), entry.FullName.Replace('/', Path.DirectorySeparatorChar));
                        var dir = Path.GetDirectoryName(path);
                        Directory.CreateDirectory(dir);

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
    }
}