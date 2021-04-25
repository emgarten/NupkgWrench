using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using McMaster.Extensions.CommandLineUtils;
using NuGet.Common;

namespace NupkgWrench
{
    internal static class EmptyFolderCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("emptyfolder", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Add an empty folder _._ placeholder to a nupkg, existing files in the folder will be removed.";
            var idFilter = cmd.Option(Constants.IdFilterTemplate, Constants.IdFilterDesc, CommandOptionType.SingleValue);
            var versionFilter = cmd.Option(Constants.VersionFilterTemplate, Constants.VersionFilterDesc, CommandOptionType.SingleValue);
            var excludeSymbolsFilter = cmd.Option(Constants.ExcludeSymbolsTemplate, Constants.ExcludeSymbolsDesc, CommandOptionType.NoValue);
            var highestVersionFilter = cmd.Option(Constants.HighestVersionFilterTemplate, Constants.HighestVersionFilterDesc, CommandOptionType.NoValue);

            cmd.HelpOption(Constants.HelpOption);
            var pathOption = cmd.Option("-p|--path", "Path within the nupkg to add an _._ file.", CommandOptionType.MultipleValue);

            var argRoot = cmd.Argument(
                "[root]",
                Constants.MultiplePackagesRootDesc,
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

                    if (fixedPath.EndsWith("/_._"))
                    {
                        fixedPath = fixedPath.Substring(0, fixedPath.Length - 4);
                    }

                    // Normalize these to be folders
                    fixedPath = fixedPath.TrimEnd('/') + '/';

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
                            Util.RemoveFiles(zip, $"{path}*", log);

                            // Add the empty file
                            using (var emptyStream = new MemoryStream(0))
                            {
                                Util.AddOrReplaceZipEntry(zip, nupkgPath, $"{path}_._", emptyStream, log);
                            }
                        }
                    }
                }

                return 0;
            });
        }
    }
}