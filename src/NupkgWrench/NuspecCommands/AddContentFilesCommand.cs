using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;
using NuGet.Packaging;

namespace NupkgWrench
{
    internal static class AddContentFilesCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("add", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Add a contentFiles entry in nuspec.";

            var idFilter = cmd.Option(Constants.IdFilterTemplate, Constants.IdFilterDesc, CommandOptionType.SingleValue);
            var versionFilter = cmd.Option(Constants.VersionFilterTemplate, Constants.VersionFilterDesc, CommandOptionType.SingleValue);
            var excludeSymbolsFilter = cmd.Option(Constants.ExcludeSymbolsTemplate, Constants.ExcludeSymbolsDesc, CommandOptionType.SingleValue);
            var highestVersionFilter = cmd.Option(Constants.HighestVersionFilterTemplate, Constants.HighestVersionFilterDesc, CommandOptionType.NoValue);

            var include = cmd.Option("--include", "content files include attribute value.", CommandOptionType.SingleValue);
            var exclude = cmd.Option("--exclude", "content files exclude attribute value.", CommandOptionType.SingleValue);
            var buildAction = cmd.Option("--build-action", "content files buildAction attribute value.", CommandOptionType.SingleValue);
            var copyToOutput = cmd.Option("--copy-to-output", "content files copyToOutput attribute value. (true|false)", CommandOptionType.SingleValue);
            var flatten = cmd.Option("--flatten", "content files flatten attribute value. (true|false)", CommandOptionType.SingleValue);

            var argRoot = cmd.Argument(
                "[root]",
                Constants.MultiplePackagesRootDesc,
                multipleValues: true);

            cmd.HelpOption(Constants.HelpOption);

            var required = new List<CommandOption>()
            {
                include
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

                        var metadata = Util.GetMetadataElement(nuspecXml);
                        var ns = metadata.GetDefaultNamespace().NamespaceName;
                        var contentFilesNode = metadata.Elements().FirstOrDefault(e => e.Name.LocalName.Equals("contentFiles", StringComparison.OrdinalIgnoreCase));

                        if (contentFilesNode == null)
                        {
                            contentFilesNode = new XElement(XName.Get("contentFiles", ns));
                            metadata.Add(contentFilesNode);
                        }

                        var entryNode = new XElement(XName.Get("files", ns));
                        entryNode.Add(new XAttribute(XName.Get("include"), include.Value()));

                        if (exclude.HasValue())
                        {
                            entryNode.Add(new XAttribute(XName.Get("exclude"), exclude.Value()));
                        }

                        if (buildAction.HasValue())
                        {
                            entryNode.Add(new XAttribute(XName.Get("buildAction"), buildAction.Value()));
                        }

                        if (copyToOutput.HasValue())
                        {
                            entryNode.Add(new XAttribute(XName.Get("copyToOutput"), copyToOutput.Value()));
                        }

                        if (flatten.HasValue())
                        {
                            entryNode.Add(new XAttribute(XName.Get("flatten"), flatten.Value()));
                        }

                        contentFilesNode.AddFirst(entryNode);

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