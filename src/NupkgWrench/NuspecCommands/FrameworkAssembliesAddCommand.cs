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
    internal static class FrameworkAssembliesAddCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("add", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Add framework assemblies to the nuspec file for desktop frameworks.";

            var idFilter = cmd.Option(Constants.IdFilterTemplate, Constants.IdFilterDesc, CommandOptionType.SingleValue);
            var versionFilter = cmd.Option(Constants.VersionFilterTemplate, Constants.VersionFilterDesc, CommandOptionType.SingleValue);
            var excludeSymbolsFilter = cmd.Option(Constants.ExcludeSymbolsTemplate, Constants.ExcludeSymbolsDesc, CommandOptionType.NoValue);
            var highestVersionFilter = cmd.Option(Constants.HighestVersionFilterTemplate, Constants.HighestVersionFilterDesc, CommandOptionType.NoValue);

            var assemblyNames = cmd.Option("-n|--assembly-name", "AssemblyName value of the FrameworkAssembly entry. May be specified multiple times.", CommandOptionType.MultipleValue);
            var targetFrameworks = cmd.Option("-f|--framework", "TargetFramework value of the FrameworkAssembly entry. If no frameworks are given this command will automatically add the reference for all desktop frameworks. Framework may be specified multiple time.", CommandOptionType.MultipleValue);
            var noFrameworks = cmd.Option("--no-frameworks", "Exclude the TargetFramework attribute.", CommandOptionType.NoValue);

            var argRoot = cmd.Argument(
                "[root]",
                Constants.MultiplePackagesRootDesc,
                multipleValues: true);

            cmd.HelpOption(Constants.HelpOption);

            cmd.OnExecute(() =>
            {
                try
                {
                    // Validate required parameters
                    ValidateCmdOptionsUtil.VerifyRequiredOptions(assemblyNames);
                    ValidateCmdOptionsUtil.VerifyMutallyExclusiveOptions(targetFrameworks, noFrameworks);

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
                        NuspecReader nuspecReader = null;
                        var packageFrameworks = new List<NuGetFramework>();
                        using (var stream = File.OpenRead(package))
                        using (var packageReader = new PackageArchiveReader(stream, leaveStreamOpen: false))
                        {
                            nuspecPath = packageReader.GetNuspecFile();
                            nuspecXml = XDocument.Load(packageReader.GetNuspec());
                            nuspecReader = packageReader.NuspecReader;

                            packageFrameworks.AddRange(packageReader.GetSupportedFrameworks().Where(e => e.IsSpecificFramework));
                        }

                        var frameworks = new HashSet<NuGetFramework>();

                        if (!noFrameworks.HasValue())
                        {
                            if (targetFrameworks.HasValue())
                            {
                                // Validate user input
                                ValidateTargetFrameworkInputs(targetFrameworks.Values);

                                // Add user input frameworks
                                frameworks.AddRange(targetFrameworks.Values.Select(e => NuGetFramework.Parse(e)));
                            }
                            else
                            {
                                frameworks.AddRange(packageFrameworks);
                            }
                        }

                        // Remove unknown frameworks and package based frameworks.
                        frameworks.RemoveWhere(e => !e.IsSpecificFramework || e.IsPackageBased);

                        var assemblyNamesUnique = new HashSet<string>(assemblyNames.Values, StringComparer.OrdinalIgnoreCase);

                        log.LogMinimal($"Adding framework assemblies: {string.Join(", ", assemblyNamesUnique)}");

                        // Modify nuspec
                        Util.AddFrameworkAssemblyReferences(nuspecXml, assemblyNamesUnique, frameworks);

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

        private static void ValidateTargetFrameworkInputs(IEnumerable<string> targetFrameworks)
        {
            foreach (var frameworkInput in targetFrameworks)
            {
                var framework = NuGetFramework.Parse(frameworkInput);

                if (!framework.IsSpecificFramework)
                {
                    throw new ArgumentException($"Invalid framework: {frameworkInput}");
                }

                if (framework.IsPackageBased)
                {
                    throw new ArgumentException($"Framework assemblies are not supported on packages based frameworks: {frameworkInput}");
                }
            }
        }
    }
}