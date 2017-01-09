using System.IO;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;
using NuGet.Packaging;

namespace NupkgWrench
{
    internal static class VersionCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("version", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Display the package version of a nupkg.";
            cmd.HelpOption(Constants.HelpOption);
            var idFilter = cmd.Option(Constants.IdFilterTemplate, Constants.IdFilterDesc, CommandOptionType.SingleValue);
            var versionFilter = cmd.Option(Constants.VersionFilterTemplate, Constants.VersionFilterDesc, CommandOptionType.SingleValue);
            var excludeSymbolsFilter = cmd.Option(Constants.ExcludeSymbolsTemplate, Constants.ExcludeSymbolsDesc, CommandOptionType.NoValue);
            var highestVersionFilter = cmd.Option(Constants.HighestVersionFilterTemplate, Constants.HighestVersionFilterDesc, CommandOptionType.NoValue);

            var normalizeOption = cmd.Option("-n|--normalize", "Normalize the version to remove leading and trailing zeros.", CommandOptionType.NoValue);

            var argRoot = cmd.Argument(
                "[root]",
                Constants.SinglePackageRootDesc,
                multipleValues: true);

            cmd.OnExecute(() =>
            {
                var inputs = argRoot.Values;

                if (inputs.Count < 1)
                {
                    inputs.Add(Directory.GetCurrentDirectory());
                }

                var nupkgPath = Util.GetSinglePackageWithFilter(idFilter, versionFilter, excludeSymbolsFilter, highestVersionFilter, inputs.ToArray());

                using (var reader = new PackageArchiveReader(nupkgPath))
                {
                    var version = reader.GetIdentity().Version;

                    if (version.IsSemVer2 || normalizeOption.HasValue())
                    {
                        log.LogMinimal(version.ToFullString());
                    }
                    else
                    {
                        log.LogMinimal(version.ToString());
                    }
                }

                return 0;
            });
        }
    }
}