using System;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Core;

namespace NupkgWrench
{
    internal static class UpdateFileNameCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("updatefilename", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Update the file name of a package to match the id and version in the nuspec.";
            cmd.HelpOption(Constants.HelpOption);

            var idFilter = cmd.Option(Constants.IdFilterTemplate, Constants.IdFilterDesc, CommandOptionType.SingleValue);
            var versionFilter = cmd.Option(Constants.VersionFilterTemplate, Constants.VersionFilterDesc, CommandOptionType.SingleValue);
            var excludeSymbolsFilter = cmd.Option(Constants.ExcludeSymbolsTemplate, Constants.ExcludeSymbolsDesc, CommandOptionType.NoValue);
            var highestVersionFilter = cmd.Option(Constants.HighestVersionFilterTemplate, Constants.HighestVersionFilterDesc, CommandOptionType.NoValue);

            var argRoot = cmd.Argument(
                "[root]",
                Constants.MultiplePackagesRootDesc,
                multipleValues: true);

            cmd.OnExecute(() =>
            {
                var inputs = argRoot.Values;

                if (inputs.Count < 1)
                {
                    inputs.Add(Directory.GetCurrentDirectory());
                }

                var packages = Util.GetPackagesWithFilter(idFilter, versionFilter, excludeSymbolsFilter, highestVersionFilter, inputs.ToArray());

                foreach (var nupkgPath in packages)
                {
                    PackageIdentity identity = null;

                    using (var reader = new PackageArchiveReader(nupkgPath))
                    {
                        identity = reader.GetIdentity();
                    }

                    var isSymbolsPackage = Util.IsSymbolPackage(nupkgPath);
                    var expectedName = Util.GetNupkgName(identity, isSymbolsPackage);
                    var currentName = Path.GetFileName(nupkgPath);

                    var dir = Path.GetDirectoryName(nupkgPath);
                    var expectedPath = Path.Combine(dir, expectedName);

                    if (StringComparer.Ordinal.Equals(expectedName, currentName))
                    {
                        log.LogMinimal($"{nupkgPath} : no changes");
                    }
                    else
                    {
                        if (File.Exists(expectedPath))
                        {
                            log.LogMinimal($"removing : {expectedPath}");
                            File.Delete(expectedPath);
                        }

                        log.LogMinimal($"{nupkgPath} : -> {expectedName}");
                        File.Move(nupkgPath, expectedPath);
                    }
                }

                return 0;
            });
        }
    }
}