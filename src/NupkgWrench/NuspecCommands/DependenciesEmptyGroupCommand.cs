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
    internal static class DependenciesEmptyGroupCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("emptygroup", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Add empty dependency groups or remove dependencies from existing groups.";

            var idFilter = cmd.Option(Constants.IdFilterTemplate, Constants.IdFilterTemplate, CommandOptionType.SingleValue);
            var versionFilter = cmd.Option(Constants.VersionFilterTemplate, Constants.VersionFilterTemplate, CommandOptionType.SingleValue);
            var excludeSymbolsFilter = cmd.Option(Constants.ExcludeSymbolsTemplate, Constants.ExcludeSymbolsDesc, CommandOptionType.SingleValue);
            var frameworkOption = cmd.Option("-f|--framework", "Group target frameworks. Use 'any' for the default group.", CommandOptionType.MultipleValue);

            var argRoot = cmd.Argument(
                "[root]",
                "Paths to individual packages or directories containing packages.",
                multipleValues: true);

            cmd.HelpOption(Constants.HelpOption);

            var required = new List<CommandOption>()
            {
                frameworkOption
            };

            cmd.OnExecute(() =>
            {
                try
                {
                    var inputs = argRoot.Values;

                    if (inputs.Count < 1)
                    {
                        inputs.Add(Directory.GetCurrentDirectory());
                    }

                    // Validate parameters
                    foreach (var requiredOption in required)
                    {
                        if (!requiredOption.HasValue())
                        {
                            throw new ArgumentException($"Missing required parameter --{requiredOption.LongName}.");
                        }
                    }

                    var frameworks = new HashSet<NuGetFramework>();

                    if (frameworkOption.HasValue())
                    {
                        foreach (var option in frameworkOption.Values)
                        {
                            var fw = NuGetFramework.Parse(option);

                            log.LogInformation($"adding empty dependency groups for {fw.GetShortFolderName()}");

                            frameworks.Add(fw);
                        }
                    }

                    var packages = Util.GetPackagesWithFilter(idFilter, versionFilter, excludeSymbolsFilter, inputs.ToArray());

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

                        if (dependenciesNode == null)
                        {
                            dependenciesNode = new XElement(XName.Get("dependencies", ns));
                            metadata.Add(dependenciesNode);
                        }

                        // Convert non-grouped to group
                        var rootDeps = dependenciesNode.Elements()
                                    .Where(e => e.Name.LocalName.Equals("dependency", StringComparison.OrdinalIgnoreCase))
                                    .ToArray();

                        if (rootDeps.Length > 1)
                        {
                            var anyGroup = new XElement(XName.Get("group", ns));
                            dependenciesNode.AddFirst(anyGroup);

                            foreach (var rootDep in rootDeps)
                            {
                                rootDep.Remove();
                                anyGroup.Add(rootDep);
                            }
                        }

                        // Remove existing groups
                        foreach (var node in dependenciesNode.Elements()
                            .Where(e => e.Name.LocalName.Equals("group", StringComparison.OrdinalIgnoreCase))
                            .ToArray())
                        {
                            var groupFramework = NuGetFramework.AnyFramework;

                            var tfm = node.Attribute(XName.Get("targetFramework"))?.Value;

                            if (!string.IsNullOrEmpty(tfm))
                            {
                                groupFramework = NuGetFramework.Parse(tfm);
                            }

                            if (frameworks.Remove(groupFramework))
                            {
                                foreach (var child in node.Elements().ToArray())
                                {
                                    child.Remove();
                                }
                            }
                        }

                        // Add empty groups for those remaining
                        foreach (var fw in frameworks)
                        {
                            var groupNode = new XElement(XName.Get("group", ns));

                            if (!fw.IsAny)
                            {
                                var version = fw.Version.ToString();

                                if (version.EndsWith(".0.0"))
                                {
                                    version = version.Substring(0, version.Length - 4);
                                }

                                if (version.EndsWith(".0")
                                 && version.IndexOf('.') != version.LastIndexOf('.'))
                                {
                                    version = version.Substring(0, version.Length - 2);
                                }

                                groupNode.Add(new XAttribute(XName.Get("targetFramework"), $"{fw.Framework}{version}"));
                            }

                            dependenciesNode.Add(groupNode);
                        }

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
