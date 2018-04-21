using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;

namespace NupkgWrench
{
    /// <summary>
    /// Add, Modify, Remove dependencies
    /// </summary>
    public static class DependenciesUtil
    {
        public static void Process(XDocument nuspecXml, EditType verb, HashSet<NuGetFramework> frameworks, string id, string version, string exclude, string include, ILogger log)
        {
            var metadata = Util.GetMetadataElement(nuspecXml);
            var nameNamespaceName = metadata.Name.NamespaceName;
            var dependenciesXName = XName.Get("dependencies", nameNamespaceName);
            var groupXName = XName.Get("group", nameNamespaceName);
            var dependencyXName = XName.Get("dependency", nameNamespaceName);
            var dependenciesNode = metadata.Element(dependenciesXName);

            // Create the dependencies node if it does not exist
            if (dependenciesNode == null && verb == EditType.Add)
            {
                dependenciesNode = new XElement(dependenciesXName);
                metadata.Add(dependenciesNode);
            }

            var groups = dependenciesNode?.Elements(groupXName).ToList() ?? new List<XElement>();

            if (verb == EditType.Add)
            {
                var rootDependencies = dependenciesNode?.Elements(dependencyXName).ToList() ?? new List<XElement>();

                // Migrate from root level dependencies to groups if needed
                if (groups.Count < 1 && rootDependencies.Count > 0)
                {
                    var group = new XElement(groupXName);
                    dependenciesNode.Add(group);
                    groups.Add(group);

                    rootDependencies.ForEach(e =>
                    {
                        e.Remove();
                        group.Add(e);
                    });
                }

                // Create a default group if adding and no frameworks exist
                if (frameworks.Count < 1 && groups.Count < 1)
                {
                    frameworks.Add(NuGetFramework.AnyFramework);
                }
            }

            // Filter groups
            if (frameworks.Count > 0)
            {
                groups.RemoveAll(e => !frameworks.Contains(e.GetFramework()));

                if (verb == EditType.Add)
                {
                    foreach (var framework in frameworks)
                    {
                        if (!groups.Any(e => framework.Equals(e.GetFramework())))
                        {
                            var group = CreateGroupNode(nameNamespaceName, framework);
                            dependenciesNode.Add(group);
                            groups.Add(group);
                        }
                    }
                }
            }

            groups.ForEach(e => ProcessDependency(e, verb, id, version, exclude, include));
        }

        public static void ProcessDependency(XElement dependencies, EditType type, string id, string version, string exclude, string include)
        {
            var metadata = Util.GetMetadataElement(dependencies.Document);
            var nameNamespaceName = metadata.Name.NamespaceName;

            var idXName = XName.Get("id");
            var versionXName = XName.Get("version");
            var excludeXName = XName.Get("exclude");
            var includeXName = XName.Get("include");

            if (dependencies == null && type == EditType.Add)
            {
                throw new ArgumentNullException(nameof(dependencies));
            }

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

                        if (include != null)
                        {
                            dependency.SetAttributeValue(includeXName, include);
                        }
                        else
                        {
                            dependency.Attribute(includeXName)
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
                        if (include != null)
                        {
                            dependency.SetAttributeValue(includeXName, include);
                        }
                        dependencies.AddFirst(dependency);
                    }
                    break;
                case EditType.Remove:
                    dependency?.Remove();
                    break;
                case EditType.Clear:
                    dependencies.Elements().ToList().ForEach(e => e.Remove());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            if (dependencies != null)
            {
                // Remove empty any groups
                if (!dependencies.Elements().Any() && dependencies.GetFramework().IsAny)
                {
                    dependencies.Remove();
                }
            }
        }

        public static XElement CreateGroupNode(string ns, NuGetFramework fw)
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

            return groupNode;
        }

        private static NuGetFramework GetFramework(this XElement node)
        {
            var targetFramework = node.Attribute(XName.Get("targetFramework"))?.Value;

            if (string.IsNullOrEmpty(targetFramework))
            {
                return NuGetFramework.AnyFramework;
            }

            return NuGetFramework.Parse(targetFramework);
        }

        public enum EditType
        {
            Add,
            Modify,
            Remove,
            Clear
        }
    }
}
