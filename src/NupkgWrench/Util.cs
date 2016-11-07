using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.FileSystemGlobbing;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Core;

namespace NupkgWrench
{
    public static class Util
    {
        /// <summary>
        /// Remove files from a zip
        /// </summary>
        public static void RemoveFiles(ZipArchive zip, string pathWildcard, ILogger log)
        {
            var entries = zip.Entries.Where(e => IsMatch(e.FullName, pathWildcard)).ToList();

            foreach (var entry in entries)
            {
                log.LogInformation($"removing : {entry.FullName}");
                entry.Delete();
            }
        }

        /// <summary>
        /// Fix slashes to match a zip entry.
        /// </summary>
        public static string GetZipPath(string path)
        {
            return path.Replace("\\", "/").TrimStart('/');
        }

        /// <summary>
        /// Add or update a root level metadata entry in a nuspec file
        /// </summary>
        public static void AddOrUpdateMetadataElement(XDocument doc, string name, string value)
        {
            var metadata = GetMetadataElement(doc);

            if (metadata == null)
            {
                throw new InvalidDataException("Invalid nuspec");
            }

            var doNotAdd = false;

            foreach (var node in metadata.Elements().Where(e => e.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray())
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    node.Remove();
                    doNotAdd = true;
                }
                else
                {
                    node.SetValue(value);
                    doNotAdd = true;
                }
            }

            if (!doNotAdd)
            {
                metadata.Add(new XElement(XName.Get(name.ToLowerInvariant(), metadata.GetDefaultNamespace().NamespaceName), value));
            }
        }

        /// <summary>
        /// Get nuspec metadata element
        /// </summary>
        public static XElement GetMetadataElement(XDocument doc)
        {
            var package = doc.Elements().FirstOrDefault(e => e.Name.LocalName.Equals("package", StringComparison.OrdinalIgnoreCase));
            return package?.Elements().FirstOrDefault(e => e.Name.LocalName.Equals("metadata", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Add or update an xml zip entry
        /// </summary>
        public static void AddOrReplaceZipEntry(string nupkgPath, string filePath, XDocument doc, ILogger log)
        {
            using (var stream = new MemoryStream())
            {
                doc.Save(stream);

                AddOrReplaceZipEntry(nupkgPath, filePath, stream, log);
            }
        }

        /// <summary>
        /// Add or update a zip entry
        /// </summary>
        public static void AddOrReplaceZipEntry(string nupkgPath, string filePath, Stream stream, ILogger log)
        {
            stream.Seek(0, SeekOrigin.Begin);

            // Get nuspec file entry
            using (var nupkgStream = File.Open(nupkgPath, FileMode.Open, FileAccess.ReadWrite))
            using (var zip = new ZipArchive(nupkgStream, ZipArchiveMode.Update))
            {
                AddOrReplaceZipEntry(zip, nupkgPath, filePath, stream, log);
            }
        }

        public static void AddOrReplaceZipEntry(ZipArchive zip, string nupkgPath, string filePath, Stream stream, ILogger log)
        {
            stream.Seek(0, SeekOrigin.Begin);

            // normalize path
            filePath = Util.GetZipPath(filePath);

            // Remove existing file
            RemoveFiles(zip, filePath, log);

            log.LogInformation($"{nupkgPath} : adding {filePath}");

            var entry = zip.CreateEntry(filePath, CompressionLevel.Optimal);
            using (var entryStream = entry.Open())
            {
                stream.CopyTo(entryStream);
            }
        }

        /// <summary>
        /// Wildcard pattern match
        /// </summary>
        public static bool IsMatch(string input, string wildcardPattern)
        {
            if (string.IsNullOrEmpty(wildcardPattern))
            {
                // Match everything if no pattern was given
                return true;
            }

            var normalizedInput = input.Replace("\\", "/").ToLowerInvariant();

            var regexPattern = Regex.Escape(wildcardPattern)
                              .Replace(@"\*", ".*")
                              .Replace(@"\?", ".");

            // Regex match, ignore case since package ids and versions are case insensitive
            return Regex.IsMatch(normalizedInput, $"^{regexPattern}$".ToLowerInvariant());
        }

        /// <summary>
        /// Filter packages down to a single package or throw.
        /// </summary>
        public static string GetSinglePackageWithFilter(
            CommandOption idFilter,
            CommandOption versionFilter,
            CommandOption excludeSymbols,
            CommandOption highestVersionFilter,
            string[] inputs)
        {
            var packages = GetPackagesWithFilter(idFilter,
                versionFilter,
                excludeSymbols,
                highestVersionFilter,
                inputs)
                .ToArray();

            if (packages.Length > 1)
            {
                var joinString = Environment.NewLine + "  - ";
                throw new ArgumentException($"This command only works for a single nupkg. The input filters given match multiple nupkgs:{joinString}{string.Join(joinString, packages)}");
            }
            else if (packages.Length < 1)
            {
                throw new ArgumentException($"This command only works for a single nupkg. The input filters given match zero nupkgs.");
            }

            return packages[0];
        }

        /// <summary>
        /// Filter packages
        /// </summary>
        public static IEnumerable<string> GetPackagesWithFilter(
            CommandOption idFilter,
            CommandOption versionFilter,
            CommandOption excludeSymbols,
            CommandOption highestVersionFilter,
            string[] inputs)
        {
            return GetPackagesWithFilter(idFilter.HasValue() ? idFilter.Value() : null,
                versionFilter.HasValue() ? versionFilter.Value() : null,
                excludeSymbols.HasValue(),
                highestVersionFilter.HasValue(),
                inputs);
        }

        /// <summary>
        /// Filter packages
        /// </summary>
        public static SortedSet<string> GetPackagesWithFilter(
            string idFilter,
            string versionFilter,
            bool excludeSymbols,
            bool highestVersionFilter,
            string[] inputs)
        {
            var files = GetPackages(inputs);

            if (!string.IsNullOrEmpty(idFilter) || !string.IsNullOrEmpty(versionFilter) || excludeSymbols)
            {
                files.RemoveWhere(path => !IsFilterMatch(idFilter, versionFilter, excludeSymbols, path));
            }

            if (highestVersionFilter)
            {
                var identities = GetPathToIdentity(files);
                var highestVersions = GetHighestVersionsById(identities);

                files = new SortedSet<string>(identities.Where(e => highestVersions.Contains(e.Value)).Select(e => e.Key));
            }

            return files;
        }

        private static HashSet<PackageIdentity> GetHighestVersionsById(Dictionary<string, PackageIdentity> identities)
        {
            var results = new HashSet<PackageIdentity>();

            foreach (var group in identities.GroupBy(e => e.Value.Id, StringComparer.OrdinalIgnoreCase))
            {
                results.Add(group.OrderByDescending(e => e.Value.Version).First().Value);
            }

            return results;
        }

        private static Dictionary<string, PackageIdentity> GetPathToIdentity(SortedSet<string> paths)
        {
            var mappings = new Dictionary<string, PackageIdentity>(StringComparer.Ordinal);

            foreach (var path in paths)
            {
                var identity = GetIdentityOrNull(path);

                // Filter out bad packages
                if (identity != null)
                {
                    mappings.Add(path, identity);
                }
            }

            return mappings;
        }

        private static bool IsFilterMatch(string idFilter, string versionFilter, bool excludeSymbols, string path)
        {
            var identity = GetIdentityOrNull(path);

            // Check all forms of the version
            if (identity != null
                && IsMatch(identity.Id, idFilter)
                && (IsMatch(identity.Version.ToString(), versionFilter)
                || IsMatch(identity.Version.ToNormalizedString(), versionFilter)
                || IsMatch(identity.Version.ToFullString(), versionFilter))
                && (!excludeSymbols || !path.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Convert inputs to a set of nupkg files.
        /// </summary>
        private static SortedSet<string> GetPackages(params string[] inputs)
        {
            var files = new SortedSet<string>(StringComparer.Ordinal);

            foreach (var input in inputs)
            {
                if (IsGlobbingPattern(input))
                {
                    // Resolver globbing pattern
                    files.AddRange(ResolveGlobbingPattern(input));
                }
                else
                {
                    // Resolve file or directory
                    var inputFile = Path.GetFullPath(input);

                    if (Directory.Exists(inputFile))
                    {
                        var directoryFiles = Directory.GetFiles(inputFile, "*.nupkg", SearchOption.AllDirectories).ToList();

                        files.UnionWith(directoryFiles);
                    }
                    else if (inputFile.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
                    {
                        if (File.Exists(inputFile))
                        {
                            files.Add(inputFile);
                        }
                        else
                        {
                            throw new FileNotFoundException($"Unable to find '{inputFile}'.");
                        }
                    }
                }
            }

            return files;
        }

        private static SortedSet<string> ResolveGlobbingPattern(string pattern)
        {
            var patternSplit = SplitGlobbingPattern(pattern);

            var matcher = new Matcher();
            matcher = matcher.AddInclude(patternSplit.Item2);

            return new SortedSet<string>(matcher.GetResultsInFullPath(patternSplit.Item1.FullName).Select(Path.GetFullPath), StringComparer.Ordinal);
        }

        public static Tuple<DirectoryInfo, string> SplitGlobbingPattern(string pattern)
        {
            var parts = pattern.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

            DirectoryInfo dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            var relativePattern = string.Empty;
            var globbingHit = false;

            for (int i=0; i < parts.Length; i++)
            {
                if (globbingHit || IsGlobbingPattern(parts[i]))
                {
                    // Append to pattern
                    globbingHit = true;
                    relativePattern = $"{relativePattern}/{parts[i]}";
                }
                else
                {
                    // Append to root dir
                    dir = new DirectoryInfo(Path.Combine(dir.FullName, parts[i]));
                }
            }

            return new Tuple<DirectoryInfo, string>(dir, relativePattern);
        }

        private static bool IsGlobbingPattern(string possiblePattern)
        {
            return possiblePattern.IndexOf("*") > -1;
        }

        public static PackageIdentity GetIdentityOrNull(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    using (var reader = new PackageArchiveReader(path))
                    {
                        return reader.GetIdentity();
                    }
                }
            }
            catch
            {
                // Ignore bad packages, these might be only partially written to disk.
                Debug.Fail("Failed to get identity");
            }

            return null;
        }
    }
}