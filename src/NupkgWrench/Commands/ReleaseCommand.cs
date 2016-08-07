using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace NupkgWrench
{
    internal static class ReleaseCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("release", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Convert a set of pre-release packages to stable or the specified version/release label. Defaults to stable.";

            var idFilter = cmd.Option("-i|--id", "Filter to only packages matching the id or wildcard.", CommandOptionType.SingleValue);
            var versionFilter = cmd.Option("-v|--version", "Filter to only packages matching the version or wildcard.", CommandOptionType.SingleValue);
            var newVersion = cmd.Option("-n|--new-version", "New version, this replaces the entire version of the nupkg. Cannot be used with --label.", CommandOptionType.SingleValue);
            var label = cmd.Option("-r|--label", "Pre-release label, this keeps the version number the same and only modifies the release label. Cannot be used with --new-version.", CommandOptionType.SingleValue);
            var stable = cmd.Option("-s|--stable", "Remove pre-release label, this is the default option.", CommandOptionType.NoValue);

            var argRoot = cmd.Argument(
                "[root]",
                "Paths to individual packages or directories containing packages.",
                multipleValues: true);

            cmd.HelpOption(Constants.HelpOption);

            cmd.OnExecute(() =>
            {
                try
                {
                    // Validate parameters
                    int optionCount = 0;

                    if (stable.HasValue())
                    {
                        optionCount++;
                    }

                    if (label.HasValue())
                    {
                        optionCount++;
                    }

                    if (newVersion.HasValue())
                    {
                        optionCount++;
                    }

                    if (optionCount > 1)
                    {
                        throw new ArgumentException($"Invalid option combination. Specify only one of the following options: {stable.LongName}, {newVersion.LongName}, {label.LongName}.");
                    }

                    var inputs = argRoot.Values;

                    if (inputs.Count < 1)
                    {
                        inputs.Add(Directory.GetCurrentDirectory());
                    }

                    // Gather all package data
                    var packages = Util.GetPackagesWithFilter(idFilter, versionFilter, inputs.ToArray());

                    var packageSet = new List<Tuple<string, PackageIdentity, string, XDocument, PackageIdentity>>();
                    var updatedIds = new HashSet<string>();

                    foreach (var package in packages)
                    {
                        log.LogMinimal($"reading {package}");

                        // Get nuspec file path
                        using (var packageReader = new PackageArchiveReader(package))
                        {
                            var nuspecPath = packageReader.GetNuspecFile();
                            var nuspecXml = XDocument.Load(packageReader.GetNuspec());
                            var identity = packageReader.GetIdentity();

                            var updatedVersion = identity.Version;

                            if (newVersion.HasValue())
                            {
                                updatedVersion = NuGetVersion.Parse(newVersion.Value());
                            }
                            else if (label.HasValue())
                            {
                                var versionPart = identity.Version.ToString().Split('-')[0];

                                updatedVersion = NuGetVersion.Parse($"{versionPart}-{label.Value()}");
                            }
                            else
                            {
                                var versionPart = identity.Version.ToString().Split('-')[0];

                                updatedVersion = NuGetVersion.Parse($"{versionPart}");
                            }

                            var newIdentity = new PackageIdentity(identity.Id, updatedVersion);

                            packageSet.Add(new Tuple<string, PackageIdentity, string, XDocument, PackageIdentity>(package, identity , nuspecPath, nuspecXml, newIdentity));

                            updatedIds.Add(identity.Id);
                        }
                    }

                    // Update dependency info
                    foreach (var package in packageSet)
                    {
                        log.LogMinimal($"processing {package.Item1}");

                        var nuspec = package.Item4;

                        // Update the version
                        Util.AddOrUpdateMetadataElement(nuspec, "version", package.Item5.Version.ToString());

                        if (package.Item2.Version != package.Item5.Version)
                        {
                            log.LogInformation($"{package.Item2.Version} -> {package.Item5.Version}");
                        }

                        var metadata = Util.GetMetadataElement(nuspec);

                        // Find all <dependency> elements
                        foreach (var dependency in metadata.DescendantNodes().Select(e => e as XElement).Where(e => e != null))
                        {
                            var depId = dependency.Attributes().FirstOrDefault(e => e.Name.LocalName.Equals("id", StringComparison.OrdinalIgnoreCase))?.Value;

                            // Check if this is a package that has been updated
                            if (updatedIds.Contains(depId))
                            {
                                // Look up the package this refers to 
                                var depPackageEntry = packageSet.First(e => e.Item2.Id.Equals(depId, StringComparison.OrdinalIgnoreCase));

                                // Get version replacement
                                var oldVersion = depPackageEntry.Item2.Version;
                                var updatedVersion = depPackageEntry.Item5.Version;

                                var rangeAttribute = dependency.Attributes().FirstOrDefault(e => e.Name.LocalName.Equals("version", StringComparison.OrdinalIgnoreCase));

                                // Modify the range if needed
                                if (rangeAttribute != null && !string.IsNullOrWhiteSpace(rangeAttribute.Value))
                                {
                                    var range = VersionRange.Parse(rangeAttribute.Value);

                                    var minVersion = range.MinVersion;
                                    var maxVersion = range.MaxVersion;
                                    var changed = false;

                                    if (VersionComparer.VersionRelease.Equals(oldVersion, minVersion))
                                    {
                                        minVersion = updatedVersion;
                                        changed = true;
                                    }

                                    if (VersionComparer.VersionRelease.Equals(oldVersion, maxVersion))
                                    {
                                        maxVersion = updatedVersion;
                                        changed = true;
                                    }

                                    if (changed)
                                    {
                                        var updatedRange = new VersionRange(minVersion, range.IsMinInclusive, maxVersion, range.IsMaxInclusive);

                                        rangeAttribute.SetValue(updatedRange.ToLegacyShortString());

                                        log.LogInformation($"dependency {depId} {range} -> {updatedRange}");
                                    }
                                }
                            }
                        }

                        // Update the nuspec file in the zip
                        Util.AddOrReplaceZipEntry(package.Item1, package.Item3, nuspec, log);

                        // Move the file
                        var newPath = Path.Combine(Path.GetDirectoryName(package.Item1), $"{package.Item5.Id}.{package.Item5.Version.ToString()}.nupkg");
                        if (!newPath.Equals(package.Item1, StringComparison.Ordinal))
                        {
                            log.LogMinimal($"{package.Item1} -> {newPath}");
                            File.Move(package.Item1, newPath);
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
