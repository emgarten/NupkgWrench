using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;

namespace NupkgWrench
{
    internal static class DependenciesClearCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("clear", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Clear all dependencies or a set of target framework group dependencies.";

            var idFilter = cmd.Option(Constants.IdFilterTemplate, Constants.IdFilterDesc, CommandOptionType.SingleValue);
            var versionFilter = cmd.Option(Constants.VersionFilterTemplate, Constants.VersionFilterDesc, CommandOptionType.SingleValue);
            var excludeSymbolsFilter = cmd.Option(Constants.ExcludeSymbolsTemplate, Constants.ExcludeSymbolsDesc, CommandOptionType.SingleValue);
            var highestVersionFilter = cmd.Option(Constants.HighestVersionFilterTemplate, Constants.HighestVersionFilterDesc, CommandOptionType.NoValue);
            var frameworkOption = cmd.Option("-f|--framework", "Group target frameworks. Use 'any' for the default group. If not specified all dependencies are removed.", CommandOptionType.MultipleValue);

            var argRoot = cmd.Argument(
                "[root]",
                Constants.MultiplePackagesRootDesc,
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

                    var removeForFrameworks = new HashSet<NuGetFramework>();

                    if (frameworkOption.HasValue())
                    {
                        foreach (var option in frameworkOption.Values)
                        {
                            var fw = NuGetFramework.Parse(option);

                            log.LogInformation($"removing dependencies for {fw.GetShortFolderName()}");

                            removeForFrameworks.Add(fw);
                        }
                    }

                    var packages = Util.GetPackagesWithFilter(idFilter, versionFilter, excludeSymbolsFilter, highestVersionFilter, inputs.ToArray());

                    foreach (var package in packages)
                    {
                        log.LogMinimal($"processing {package}");

                        // Get nuspec file path
                        string nuspecPath = null;
                        XDocument nuspecXml = null;
                        using (var packageReader = new PackageArchiveReader(package))
                        {
                            nuspecPath = packageReader.GetNuspecFile();
                            nuspecXml = XDocument.Load(packageReader.GetNuspec());
                        }

                        var metadata = Util.GetMetadataElement(nuspecXml);
                        var ns = metadata.GetDefaultNamespace().NamespaceName;
                        var dependenciesNode = metadata.Elements().FirstOrDefault(e => e.Name.LocalName.Equals("dependencies", StringComparison.OrdinalIgnoreCase));

                        if (dependenciesNode != null)
                        {
                            if (removeForFrameworks.Count < 1)
                            {
                                dependenciesNode.Remove();
                            }
                            else
                            {
                                foreach (var fw in removeForFrameworks)
                                {
                                    if (fw.IsAny)
                                    {
                                        // Remove non-group items
                                        foreach (var node in dependenciesNode.Elements()
                                            .Where(e => e.Name.LocalName.Equals("dependency", StringComparison.OrdinalIgnoreCase))
                                            .ToArray())
                                        {
                                            node.Remove();
                                        }

                                        // Remove groups with no tfm
                                        foreach (var node in dependenciesNode.Elements()
                                            .Where(e => e.Name.LocalName.Equals("group", StringComparison.OrdinalIgnoreCase)
                                                && !e.Attributes(XName.Get("targetFramework")).Any())
                                            .ToArray())
                                        {
                                            node.Remove();
                                        }
                                    }
                                    else
                                    {
                                        foreach (var node in dependenciesNode.Elements()
                                            .Where(e => e.Name.LocalName.Equals("group", StringComparison.OrdinalIgnoreCase))
                                            .ToArray())
                                        {
                                            var tfm = node.Attribute(XName.Get("targetFramework"))?.Value;

                                            if (!string.IsNullOrEmpty(tfm))
                                            {
                                                var framework = NuGetFramework.Parse(tfm);

                                                if (framework.Equals(fw))
                                                {
                                                    // Remove matching nodes
                                                    node.Remove();
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            // Update zip
                            Util.AddOrReplaceZipEntry(package, nuspecPath, nuspecXml, log);
                        }
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