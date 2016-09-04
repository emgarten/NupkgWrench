using System;
using System.IO;
using System.Xml.Linq;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;
using NuGet.Packaging;

namespace NupkgWrench
{
    internal static class FrameworkAssembliesClearCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("clear", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Clear all framework assemblies from a nuspec file.";

            var idFilter = cmd.Option(Constants.IdFilterTemplate, Constants.IdFilterTemplate, CommandOptionType.SingleValue);
            var versionFilter = cmd.Option(Constants.VersionFilterTemplate, Constants.VersionFilterTemplate, CommandOptionType.SingleValue);
            var excludeSymbolsFilter = cmd.Option(Constants.ExcludeSymbolsTemplate, Constants.ExcludeSymbolsDesc, CommandOptionType.SingleValue);

            var argRoot = cmd.Argument(
                "[root]",
                "Paths to individual packages or directories containing packages.",
                multipleValues: true);

            cmd.HelpOption(Constants.HelpOption);

            cmd.OnExecute(() =>
            {
                try
                {
                    var inputs = argRoot.Values;

                    if (inputs.Count < 1)
                    {
                        inputs.Add(Directory.GetCurrentDirectory());
                    }

                    var packages = Util.GetPackagesWithFilter(idFilter, versionFilter, excludeSymbolsFilter, inputs.ToArray());

                    foreach (var package in packages)
                    {
                        log.LogMinimal($"modifying {package}");

                        // Get nuspec file path
                        string nuspecPath = null;
                        XDocument nuspecXml = null;
                        using (var packageReader = new PackageArchiveReader(package))
                        {
                            nuspecPath = packageReader.GetNuspecFile();
                            nuspecXml = XDocument.Load(packageReader.GetNuspec());
                        }

                        // Remove node
                        Util.AddOrUpdateMetadataElement(nuspecXml, "frameworkAssemblies", value: null);

                        // Update zip
                        Util.AddOrReplaceZipEntry(package, nuspecPath, nuspecXml, log);
                    }

                    return 0;
                }
                catch (Exception ex)
                {
                    log.LogError(ex.Message);
                    log.LogDebug(ex.ToString());
                }

                return 1;
            });
        }
    }
}