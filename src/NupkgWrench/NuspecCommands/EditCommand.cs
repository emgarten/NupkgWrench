using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using McMaster.Extensions.CommandLineUtils;
using NuGet.Common;
using NuGet.Packaging;

namespace NupkgWrench
{
    internal static class EditCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("edit", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Modifies or adds a top level property to the nuspec in a package.";

            var idFilter = cmd.Option(Constants.IdFilterTemplate, Constants.IdFilterDesc, CommandOptionType.SingleValue);
            var versionFilter = cmd.Option(Constants.VersionFilterTemplate, Constants.VersionFilterDesc, CommandOptionType.SingleValue);
            var excludeSymbolsFilter = cmd.Option(Constants.ExcludeSymbolsTemplate, Constants.ExcludeSymbolsDesc, CommandOptionType.NoValue);
            var highestVersionFilter = cmd.Option(Constants.HighestVersionFilterTemplate, Constants.HighestVersionFilterDesc, CommandOptionType.NoValue);

            var property = cmd.Option("-p|--property", "XML node name ex: Id, Version, Authors.", CommandOptionType.SingleValue);
            var propertyValue = cmd.Option("-s|--value", "XML node value.", CommandOptionType.SingleValue);

            var argRoot = cmd.Argument(
                "[root]",
                Constants.MultiplePackagesRootDesc,
                multipleValues: true);

            cmd.HelpOption(Constants.HelpOption);

            var required = new List<CommandOption>()
            {
                property,
                propertyValue
            };

            cmd.OnExecute(() =>
            {
                try
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

                    var packages = Util.GetPackagesWithFilter(idFilter, versionFilter, excludeSymbolsFilter, highestVersionFilter, inputs.ToArray());

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

                        // Modify value
                        Util.AddOrUpdateMetadataElement(nuspecXml, property.Value(), propertyValue.Value());

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