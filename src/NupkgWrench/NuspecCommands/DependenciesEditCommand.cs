using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using McMaster.Extensions.CommandLineUtils;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;

namespace NupkgWrench
{
    internal static class DependenciesEditCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("edit", cmd => Run(cmd, log));
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Edit all dependencies or set of target framework group dependencies.";

            var idFilter = cmd.Option(Constants.IdFilterTemplate, Constants.IdFilterDesc, CommandOptionType.SingleValue);
            var versionFilter = cmd.Option(Constants.VersionFilterTemplate, Constants.VersionFilterDesc, CommandOptionType.SingleValue);
            var frameworkOption = cmd.Option(Constants.FrameworkOptionTemplate, Constants.FrameworkOptionDesc, CommandOptionType.MultipleValue);

            var editType = cmd.Option(Constants.EditTypeTemplate, Constants.EditTypeDesc, CommandOptionType.SingleValue);
            var editExclude = cmd.Option(Constants.EditExcludeTemplate, Constants.EditExcludeDesc, CommandOptionType.SingleValue);

            var argRoot = cmd.Argument(
                "[root]",
                Constants.MultiplePackagesRootDesc,
                true);

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

                    var editForFrameworks = new HashSet<NuGetFramework>();

                    if (frameworkOption.HasValue())
                    {
                        foreach (var option in frameworkOption.Values)
                        {
                            var nugetFramework = NuGetFramework.Parse(option);

                            log.LogInformation($"editing dependencies for {nugetFramework.GetShortFolderName()}");

                            editForFrameworks.Add(nugetFramework);
                        }
                    }

                    if (!editType.HasValue())
                    {
                        log.LogError("please provider edit verb option.");
                        return 1;
                    }

                    if (!Enum.TryParse(editType.Value(), true, out EditType verb))
                    {
                        log.LogError($"incorrect edit verb: {editType.Value()}.");
                        return 1;
                    }

                    if (!idFilter.HasValue())
                    {
                        log.LogError("please provider id option.");
                        return 1;
                    }

                    if (!versionFilter.HasValue())
                    {
                        log.LogError("please provider version option.");
                        return 1;
                    }

                    var version = versionFilter.Value();
                    var exclude = editExclude.HasValue() ? editExclude.Value() : null;

                    var packages = Util.GetPackagesWithFilter(null, null, false, false, inputs.ToArray());

                    foreach (var package in packages)
                    {
                        var id = idFilter.Value();

                        log.LogMinimal($"processing {package}");

                        // Get nuspec file path
                        string nuspecPath;
                        XDocument nuspecXml;
                        using (var packageReader = new PackageArchiveReader(package))
                        {
                            nuspecPath = packageReader.GetNuspecFile();
                            nuspecXml = XDocument.Load(packageReader.GetNuspec());
                        }

                        var metadata = Util.GetMetadataElement(nuspecXml);
                        var nameNamespaceName = metadata.Name.NamespaceName;
                        var dependenciesNode = metadata.Element(XName.Get("dependencies", nameNamespaceName));

                        if (dependenciesNode != null)
                        {
                            var groups = dependenciesNode.Elements(XName.Get("group", nameNamespaceName)).ToList();
                            if (editForFrameworks.Count < 1)
                            {
                                if (groups.Count > 0)
                                {
                                    foreach (var element in groups)
                                    {
                                        Process(element, verb, id, version, nameNamespaceName, exclude);
                                    }
                                }
                                else
                                {
                                    Process(dependenciesNode, verb, id, version, nameNamespaceName, exclude);
                                }
                            }
                            else
                            {
                                foreach (var editForFramework in editForFrameworks)
                                {
                                    if (editForFramework.IsAny)
                                    {
                                        if (groups.Count > 0)
                                        {
                                            foreach (var element in groups)
                                            {
                                                // Process groups with no tfm
                                                Process(element, verb, id, version, nameNamespaceName, exclude);
                                            }
                                        }
                                        else
                                        {
                                            // Process non-group items
                                            Process(dependenciesNode, verb, id, version, nameNamespaceName, exclude);
                                        }
                                    }
                                    else
                                    {
                                        foreach (var node in groups)
                                        {
                                            var targetFramework = node.Attribute(XName.Get("targetFramework"))?.Value;

                                            if (!string.IsNullOrEmpty(targetFramework))
                                            {
                                                var framework = NuGetFramework.Parse(targetFramework);

                                                if (framework.Equals(editForFramework))
                                                {
                                                    Process(node, verb, id, version, nameNamespaceName, exclude);
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
                    log.LogError(ex.ToString());
                }

                return 1;
            });
        }

        private static void Process(XContainer dependencies, EditType type, string id, string version, string nameNamespaceName, string exclude)
        {
            var idXName = XName.Get("id");
            var versionXName = XName.Get("version");
            var excludeXName = XName.Get("exclude");
            var dependency = dependencies?.Elements(XName.Get("dependency", nameNamespaceName))
                .FirstOrDefault(e => string.Equals(e.Attribute(idXName)?.Value, id, StringComparison.OrdinalIgnoreCase));
            switch (type)
            {
                case EditType.Add:
                case EditType.Modify:
                    if (dependency != null)
                    {
                        dependency.SetAttributeValue(versionXName, version);
                        if (exclude != null)
                        {
                            dependency.SetAttributeValue(excludeXName, exclude);
                        }
                        else
                        {
                            dependency.Attribute(excludeXName)
                                ?.Remove();
                        }
                    }
                    else if (dependencies != null)
                    {
                        dependency = new XElement(XName.Get("dependency", nameNamespaceName));
                        dependency.SetAttributeValue(idXName, id);
                        dependency.SetAttributeValue(versionXName, version);
                        if (exclude != null)
                        {
                            dependency.SetAttributeValue(excludeXName, exclude);
                        }
                        dependencies.AddFirst(dependency);
                    }
                    break;
                case EditType.Remove:
                    dependency?.Remove();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }

    public enum EditType
    {
        Add,
        Modify,
        Remove
    }
}