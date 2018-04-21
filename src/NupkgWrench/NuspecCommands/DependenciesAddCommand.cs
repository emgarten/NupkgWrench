using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using NuGet.Common;
using NuGet.Frameworks;

namespace NupkgWrench
{
    internal static class DependenciesAddCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("add", cmd => Run(cmd, log));
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Add package dependencies";

            // Filters
            var idFilter = cmd.Option(Constants.IdFilterTemplate, Constants.IdFilterDesc, CommandOptionType.SingleValue);
            var versionFilter = cmd.Option(Constants.VersionFilterTemplate, Constants.VersionFilterDesc, CommandOptionType.SingleValue);
            var excludeSymbolsFilter = cmd.Option(Constants.ExcludeSymbolsTemplate, Constants.ExcludeSymbolsDesc, CommandOptionType.NoValue);
            var highestVersionFilter = cmd.Option(Constants.HighestVersionFilterTemplate, Constants.HighestVersionFilterDesc, CommandOptionType.NoValue);

            // Command options
            var frameworkOption = cmd.Option(Constants.FrameworkOptionTemplate, Constants.FrameworkOptionDesc, CommandOptionType.MultipleValue);
            var editExclude = cmd.Option(Constants.DependencyExcludeAttributeTemplate, Constants.DependencyExcludeAttributeDesc, CommandOptionType.SingleValue);
            var editInclude = cmd.Option(Constants.DependencyIncludeAttributeTemplate, Constants.DependencyIncludeAttributeDesc, CommandOptionType.SingleValue);
            var dependencyVersionOption = cmd.Option(Constants.DependencyVersionRangeTemplate, Constants.DependencyVersionRangeDesc, CommandOptionType.SingleValue);
            var dependencyIdOption = cmd.Option(Constants.DependencyIdTemplate, Constants.DependencyIdDesc, CommandOptionType.SingleValue);

            var argRoot = cmd.Argument(
                "[root]",
                Constants.MultiplePackagesRootDesc,
                true);

            cmd.HelpOption(Constants.HelpOption);

            cmd.OnExecute(() =>
            {
                try
                {
                    // Validate parameteres
                    CmdUtils.VerifyRequiredOptions(dependencyIdOption, dependencyVersionOption);

                    var inputs = argRoot.Values;

                    if (inputs.Count < 1)
                    {
                        inputs.Add(Directory.GetCurrentDirectory());
                    }

                    var dependencyId = dependencyIdOption.Value();

                    var editForFrameworks = new HashSet<NuGetFramework>();
                    if (frameworkOption.HasValue())
                    {
                        editForFrameworks.UnionWith(frameworkOption.Values.Select(NuGetFramework.Parse));

                        log.LogInformation($"adding dependency {dependencyId} to {string.Join(", ", editForFrameworks.Select(e => e.GetShortFolderName()))}");
                    }
                    else
                    {
                        log.LogInformation($"adding dependency {dependencyId} to all frameworks");
                    }

                    var version = dependencyVersionOption.Value();
                    var exclude = editExclude.HasValue() ? editExclude.Value() : null;
                    var include = editInclude.HasValue() ? editInclude.Value() : null;

                    var packages = Util.GetPackagesWithFilter(idFilter, versionFilter, excludeSymbolsFilter, highestVersionFilter, inputs.ToArray());

                    foreach (var package in packages)
                    {
                        log.LogMinimal($"processing {package}");

                        var nuspecXml = Util.GetNuspec(package);

                        DependenciesUtil.Process(nuspecXml, DependenciesUtil.EditType.Add, editForFrameworks, dependencyId, version, exclude, include, log);

                        Util.ReplaceNuspec(package, nuspecXml, log);
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
    }
}