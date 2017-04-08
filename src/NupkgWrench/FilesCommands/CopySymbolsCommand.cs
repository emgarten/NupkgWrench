using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Core;

namespace NupkgWrench
{
    internal static class CopySymbolsCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("copysymbols", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Copy pdb symbol files from .symbols.nupkg files into the primary package next to the matching dlls.";
            cmd.HelpOption(Constants.HelpOption);
            var deleteOption = cmd.Option("-d|--delete-symbols-nupkg", "Delete .symbols.nupkg after copy.", CommandOptionType.NoValue);

            var idFilter = cmd.Option(Constants.IdFilterTemplate, Constants.IdFilterDesc, CommandOptionType.SingleValue);
            var versionFilter = cmd.Option(Constants.VersionFilterTemplate, Constants.VersionFilterDesc, CommandOptionType.SingleValue);
            var highestVersionFilter = cmd.Option(Constants.HighestVersionFilterTemplate, Constants.HighestVersionFilterDesc, CommandOptionType.NoValue);

            var argRoot = cmd.Argument(
                "[root]",
                Constants.MultiplePackagesRootDesc,
                multipleValues: true);

            cmd.OnExecute(() =>
            {
                var inputs = argRoot.Values;

                if (inputs.Count < 1)
                {
                    inputs.Add(Directory.GetCurrentDirectory());
                }

                // Gather all package data
                var packages = Util.GetPackagesWithFilter(idFilter, versionFilter, new CommandOption(Constants.ExcludeSymbolsTemplate, CommandOptionType.NoValue), highestVersionFilter, inputs.ToArray()).ToList();

                var symbolsPackages = Util.GetPathToIdentity(new SortedSet<string>(packages.Where(e => Util.IsSymbolPackage(e))));
                var primaryPackages = Util.GetPathToIdentity(new SortedSet<string>(packages.Where(e => !Util.IsSymbolPackage(e))));

                var symbolsIds = new HashSet<PackageIdentity>(symbolsPackages.Values);
                var primaryIds = new HashSet<PackageIdentity>(primaryPackages.Values);

                foreach (var missing in symbolsIds.Except(primaryIds))
                {
                    log.LogWarning($"Missing primary package for {missing}");
                }

                foreach (var missing in primaryIds.Except(symbolsIds))
                {
                    log.LogWarning($"Missing symbols package for {missing}");
                }

                // Copy symbols
                var pairs = GetSymbolPackagePairs(primaryPackages, symbolsPackages);

                foreach (var pair in pairs)
                {
                    CopySymbolsToNupkg(pair.Key, pair.Value, log);

                    if (deleteOption.HasValue())
                    {
                        log.LogInformation($"Removing {pair.Value}");
                        File.Delete(pair.Value);
                    }
                }

                return 0;
            });
        }

        /// <summary>
        /// Copy pdbs to a nupkg.
        /// </summary>
        private static void CopySymbolsToNupkg(string packagePath, string symbolsPath, ILogger log)
        {
            var filePathsWithoutExtension = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Read main nupkg
            using (var reader = new PackageArchiveReader(packagePath))
            {
                filePathsWithoutExtension.UnionWith(reader.GetFiles().Select(Util.RemoveFileExtensionFromPath));
            }

            using (var zipStream = File.Open(packagePath, FileMode.Open))
            using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Update, leaveOpen: false))
            using (var symbolsReader = new PackageArchiveReader(symbolsPath))
            {
                var pdbs = symbolsReader.GetFiles().Where(e => StringComparer.OrdinalIgnoreCase.Equals(".pdb", Path.GetExtension(e)));

                foreach (var pdbFile in pdbs)
                {
                    // Copy pdb to nupkg if there is a matching file
                    if (filePathsWithoutExtension.Contains(Util.RemoveFileExtensionFromPath(pdbFile)))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            symbolsReader.GetStream(pdbFile).CopyTo(memoryStream);

                            Util.AddOrReplaceZipEntry(zip, packagePath, pdbFile, memoryStream, log);
                        }
                    }
                }
            }
        }

        private static List<KeyValuePair<string, string>> GetSymbolPackagePairs(
            Dictionary<string, PackageIdentity> primaryPackages,
            Dictionary<string, PackageIdentity> symbolsPackages)
        {
            return primaryPackages.Select(pkg => new KeyValuePair<string, string>(
                key: pkg.Key,
                value: symbolsPackages.Where(e => e.Value.Equals(pkg.Value))
                                                    .Select(e => e.Key)
                                                    .FirstOrDefault()))
                           .Where(e => e.Value != null)
                           .ToList();
        }
    }
}