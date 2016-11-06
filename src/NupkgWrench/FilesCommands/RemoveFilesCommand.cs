using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;

namespace NupkgWrench
{
    internal static class RemoveFilesCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("remove", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Remove files from a nupkg.";
            cmd.HelpOption(Constants.HelpOption);
            var pathOption = cmd.Option("-p|--path", "Paths to remove. These may include wildcards.", CommandOptionType.MultipleValue);
            var idFilter = cmd.Option(Constants.IdFilterTemplate, Constants.IdFilterDesc, CommandOptionType.SingleValue);
            var versionFilter = cmd.Option(Constants.VersionFilterTemplate, Constants.VersionFilterDesc, CommandOptionType.SingleValue);
            var excludeSymbolsFilter = cmd.Option(Constants.ExcludeSymbolsTemplate, Constants.ExcludeSymbolsDesc, CommandOptionType.SingleValue);
            var highestVersionFilter = cmd.Option(Constants.HighestVersionFilterTemplate, Constants.HighestVersionFilterDesc, CommandOptionType.NoValue);

            var argRoot = cmd.Argument(
                "[root]",
                "Paths to individual packages or directories containing packages.",
                multipleValues: true);

            var required = new List<CommandOption>()
            {
                pathOption
            };

            cmd.OnExecute(() =>
            {
                var inputs = argRoot.Values;

                if (inputs.Count < 1)
                {
                    inputs.Add(Directory.GetCurrentDirectory());
                }

                // Gather all package data
                var packages = Util.GetPackagesWithFilter(idFilter, versionFilter, excludeSymbolsFilter, highestVersionFilter, inputs.ToArray());

                // Validate parameters
                foreach (var requiredOption in required)
                {
                    if (!requiredOption.HasValue())
                    {
                        throw new ArgumentException($"Missing required parameter --{requiredOption.LongName}.");
                    }
                }

                var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var path in pathOption.Values)
                {
                    var fixedPath = Util.GetZipPath(path);
                    paths.Add(fixedPath);
                }

                foreach (var nupkgPath in packages)
                {
                    using (var stream = File.Open(nupkgPath, FileMode.Open))
                    using (var zip = new ZipArchive(stream, ZipArchiveMode.Update))
                    {
                        foreach (var path in paths)
                        {
                            // Remove any existing files
                            Util.RemoveFiles(zip, path, log);
                        }
                    }
                }

                return 0;
            });
        }
    }
}