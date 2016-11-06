using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            cmd.Description = "Convert a set of pre-release packages to stable or the specified version/release label. Package dependencies will also be modified to match. Defaults to stable.";

            var idFilter = cmd.Option(Constants.IdFilterTemplate, Constants.IdFilterDesc, CommandOptionType.SingleValue);
            var versionFilter = cmd.Option(Constants.VersionFilterTemplate, Constants.VersionFilterDesc, CommandOptionType.SingleValue);
            var excludeSymbolsFilter = cmd.Option(Constants.ExcludeSymbolsTemplate, Constants.ExcludeSymbolsDesc, CommandOptionType.SingleValue);
            var highestVersionFilter = cmd.Option(Constants.HighestVersionFilterTemplate, Constants.HighestVersionFilterDesc, CommandOptionType.NoValue);

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
                    var packages = Util.GetPackagesWithFilter(idFilter, versionFilter, excludeSymbolsFilter, highestVersionFilter, inputs.ToArray());

                    var packageSet = new List<Tuple<string, PackageIdentity, string, XDocument, PackageIdentity>>();
                    var updatedIds = new HashSet<string>();
                    var fileNameUpdates = new Dictionary<string, string>(StringComparer.Ordinal);

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

                            packageSet.Add(new Tuple<string, PackageIdentity, string, XDocument, PackageIdentity>(package, identity, nuspecPath, nuspecXml, newIdentity));

                            updatedIds.Add(identity.Id);

                            // Determine the new file name
                            var newFileName = $"{newIdentity.Id}.{newIdentity.Version.ToString()}.nupkg";

                            if (package.EndsWith(".symbols.nupkg"))
                            {
                                newFileName = $"{newIdentity.Id}.{newIdentity.Version.ToString()}.symbols.nupkg";
                            }

                            var rootDir = Path.GetDirectoryName(package);
                            var newFullPath = Path.Combine(rootDir, newFileName);

                            // Old path -> New path
                            fileNameUpdates.Add(package, newFullPath);
                        }
                    }

                    // Verify there are no collisions
                    VerifyNoConflicts(fileNameUpdates);

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
                                var rangeAttribute = dependency.Attributes().FirstOrDefault(e => e.Name.LocalName.Equals("version", StringComparison.OrdinalIgnoreCase));

                                // Modify the range if needed
                                if (rangeAttribute != null && !string.IsNullOrWhiteSpace(rangeAttribute.Value))
                                {
                                    var range = VersionRange.Parse(rangeAttribute.Value);

                                    // Verify the original range is valid.
                                    if (range.HasLowerAndUpperBounds
                                        && VersionComparer.VersionRelease.Compare(range.MinVersion, range.MaxVersion) > 0)
                                    {
                                        log.LogWarning($"dependency range is invalid: {depId} {rangeAttribute.Value}. Skipping.");
                                        continue;
                                    }

                                    // Look up the package this refers to
                                    // If there are multiple packages of the same id apply the update 
                                    // for the lowest version, favoring one with an original version 
                                    // that matched the original range.
                                    // Ordering by the original version is used just as a tie breaker
                                    var depPackageEntry = packageSet
                                        .Where(e => e.Item2.Id.Equals(depId, StringComparison.OrdinalIgnoreCase))
                                        .OrderBy(e => range.Satisfies(e.Item2.Version) ? 0 : 1)
                                        .ThenBy(e => e.Item5.Version)
                                        .ThenBy(e => e.Item2.Version)
                                        .First();

                                    // Get version replacement
                                    var oldVersion = depPackageEntry.Item2.Version;
                                    var updatedVersion = depPackageEntry.Item5.Version;

                                    // Verify the original package version was part of the original range.
                                    if (!range.Satisfies(oldVersion))
                                    {
                                        log.LogWarning($"dependency {depId} does not allow the original version of {depId} {oldVersion.ToNormalizedString()}. Skipping.");
                                        continue;
                                    }

                                    var minVersion = range.MinVersion;
                                    var maxVersion = range.MaxVersion;
                                    var includeMin = range.IsMinInclusive;
                                    var includeMax = range.IsMaxInclusive;
                                    var changed = false;

                                    // Always update the min version if the range includes one
                                    if (minVersion != null
                                        && (!VersionComparer.VersionRelease.Equals(updatedVersion, minVersion)
                                            || !includeMin))
                                    {
                                        minVersion = updatedVersion;
                                        includeMin = true;
                                        changed = true;
                                    }

                                    // Update the max if the original max matches the original version,
                                    // or if the new version is above the old max.
                                    if (maxVersion != null
                                        && (VersionComparer.VersionRelease.Compare(oldVersion, maxVersion) == 0
                                            || VersionComparer.VersionRelease.Compare(updatedVersion, maxVersion) >= 0))
                                    {
                                        maxVersion = updatedVersion;
                                        includeMax = true;
                                        changed = true;
                                    }

                                    if (changed)
                                    {
                                        // Create new range
                                        var updatedRange = new VersionRange(
                                                minVersion: minVersion,
                                                includeMinVersion: includeMin,
                                                maxVersion: maxVersion,
                                                includeMaxVersion: includeMax);

                                        // Verify the new version is allowed by the new range.
                                        if (!updatedRange.Satisfies(updatedVersion))
                                        {
                                            if (range.HasUpperBound)
                                            {
                                                // Lock to only the new version
                                                minVersion = updatedVersion;
                                                maxVersion = updatedVersion;
                                                includeMin = true;
                                                includeMax = true;

                                                log.LogWarning($"Locking dependency range for {depId} to = {minVersion.ToNormalizedString()}.");
                                            }
                                            else
                                            {
                                                // If there was no max, update to >= updatedVersion
                                                minVersion = updatedVersion;
                                                maxVersion = null;
                                                includeMin = true;
                                                includeMax = false;

                                                log.LogWarning($"Resetting dependency range for {depId} to >= {minVersion.ToNormalizedString()}.");
                                            }
                                        }

                                        rangeAttribute.SetValue(updatedRange.ToLegacyShortString());

                                        log.LogInformation($"dependency {depId} {range} -> {updatedRange}");
                                    }
                                }
                            }
                        }

                        // Update the nuspec file in the zip
                        Util.AddOrReplaceZipEntry(package.Item1, package.Item3, nuspec, log);

                        // Move the file
                        var newPath = fileNameUpdates[package.Item1];
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

        private static void VerifyNoConflicts(Dictionary<string, string> fileNameUpdates)
        {
            var conflicts = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

            foreach (var pair in fileNameUpdates)
            {
                HashSet<string> matches;
                if (!conflicts.TryGetValue(pair.Value, out matches))
                {
                    matches = new HashSet<string>(StringComparer.Ordinal);
                    conflicts.Add(pair.Value, matches);
                }

                matches.Add(pair.Key);
            }

            foreach (var pair in conflicts)
            {
                if (pair.Value.Count > 1)
                {
                    throw new InvalidOperationException($"Output file name collision on {pair.Key}. Inputs: {string.Join(", ", pair.Value)}");
                }
            }
        }
    }
}