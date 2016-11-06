using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;

namespace NupkgWrench
{
    internal static class ListFilesCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("list", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "List files inside a nupkg.";
            cmd.HelpOption(Constants.HelpOption);
            var idFilter = cmd.Option(Constants.IdFilterTemplate, Constants.IdFilterDesc, CommandOptionType.SingleValue);
            var versionFilter = cmd.Option(Constants.VersionFilterTemplate, Constants.VersionFilterDesc, CommandOptionType.SingleValue);
            var excludeSymbolsFilter = cmd.Option(Constants.ExcludeSymbolsTemplate, Constants.ExcludeSymbolsDesc, CommandOptionType.SingleValue);
            var highestVersionFilter = cmd.Option(Constants.HighestVersionFilterTemplate, Constants.HighestVersionFilterDesc, CommandOptionType.NoValue);

            var argRoot = cmd.Argument(
                "[root]",
                "Path to an individual package or directory containing a single package.",
                multipleValues: true);

            cmd.OnExecute(() =>
            {
                var inputs = argRoot.Values;

                if (inputs.Count < 1)
                {
                    inputs.Add(Directory.GetCurrentDirectory());
                }

                var nupkgPath = Util.GetSinglePackageWithFilter(idFilter, versionFilter, excludeSymbolsFilter, highestVersionFilter, inputs.ToArray());

                using (var stream = File.OpenRead(nupkgPath))
                using (var zip = new ZipArchive(stream))
                {
                    foreach (var entry in zip.Entries.Select(e => e.FullName).OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
                    {
                        log.LogMinimal(entry);
                    }
                }

                return 0;
            });
        }
    }
}