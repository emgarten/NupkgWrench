using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Core;

namespace NupkgWrench
{
    public static class Util
    {
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

            var added = false;

            foreach (var node in metadata.Elements().Where(e => e.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray())
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    node.Remove();
                }
                else
                {
                    node.SetValue(value);
                    added = true;
                }
            }

            if (!added)
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
                var exists = zip.Entries.Any(e => e.FullName.Equals(filePath, StringComparison.OrdinalIgnoreCase));

                ZipArchiveEntry entry = null;

                if (exists)
                {
                    entry = zip.GetEntry(filePath);

                    // Correct casing if needed
                    filePath = entry.FullName;
                    entry.Delete();
                }

                log.LogInformation($"{nupkgPath} : updating {filePath}");

                entry = zip.CreateEntry(filePath, CompressionLevel.Optimal);
                using (var entryStream = entry.Open())
                {
                    stream.CopyTo(entryStream);
                }
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

            var regexPattern = Regex.Escape(wildcardPattern)
                              .Replace(@"\*", ".*")
                              .Replace(@"\?", ".");

            // Regex match, ignore case since package ids and versions are case insensitive
            return Regex.IsMatch(input.ToLowerInvariant(), $"^{wildcardPattern}$".ToLowerInvariant());
        }

        /// <summary>
        /// Filter packages
        /// </summary>
        public static IEnumerable<string> GetPackagesWithFilter(CommandOption idFilter, CommandOption versionFilter, string[] inputs)
        {
            return GetPackagesWithFilter(idFilter.HasValue() ? idFilter.Value() : null,
                versionFilter.HasValue() ? versionFilter.Value() : null,
                inputs);
        }

        /// <summary>
        /// Filter packages
        /// </summary>
        public static IEnumerable<string> GetPackagesWithFilter(string idFilter, string versionFilter, string[] inputs)
        {
            foreach (var path in GetPackages(inputs))
            {
                if (string.IsNullOrEmpty(idFilter) && string.IsNullOrEmpty(versionFilter))
                {
                    yield return path;
                }
                else
                {
                    using (var reader = new PackageArchiveReader(path))
                    {
                        var identity = reader.GetIdentity();

                        // Check all forms of the version
                        if (IsMatch(identity.Id, idFilter)
                            && (IsMatch(identity.Version.ToString(), versionFilter)
                            || IsMatch(identity.Version.ToNormalizedString(), versionFilter)
                            || IsMatch(identity.Version.ToFullString(), versionFilter)))
                        {
                            yield return path;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Convert inputs to a set of nupkg files.
        /// </summary>
        public static SortedSet<string> GetPackages(params string[] inputs)
        {
            var files = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var input in inputs)
            {
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

            return files;
        }
    }
}
