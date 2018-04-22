using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using NuGet.Common;
using NuGet.Frameworks;

namespace NupkgWrench
{
    internal static class DependenciesModifyCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("modify", cmd => Run(cmd, log));
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Modify package dependencies. If no dependency id is specified all dependencies matched will be modified to the version, exclude, or include given.";

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

            var clearExclude = cmd.Option("--clear-exclude", "Clear exclude attribute.", CommandOptionType.NoValue);
            var clearInclude = cmd.Option("--clear-include", "Clear include attribute.", CommandOptionType.NoValue);

            var argRoot = cmd.Argument(
                "[root]",
                Constants.MultiplePackagesRootDesc,
                true);

            cmd.HelpOption(Constants.HelpOption);

            cmd.OnExecute(() =>
            {
                try
                {
                    // Validate parameters
                    CmdUtils.VerifyOneOptionExists(editInclude, editExclude, dependencyVersionOption, clearInclude, clearExclude);
                    CmdUtils.VerifyMutallyExclusiveOptions(editExclude, clearExclude);
                    CmdUtils.VerifyMutallyExclusiveOptions(editInclude, clearInclude);

                    var inputs = argRoot.Values;

                    if (inputs.Count < 1)
                    {
                        inputs.Add(Directory.GetCurrentDirectory());
                    }

                    var dependencyId = dependencyIdOption.HasValue() ? dependencyIdOption.Value() : null;

                    var editForFrameworks = new HashSet<NuGetFramework>();
                    if (frameworkOption.HasValue())
                    {
                        editForFrameworks.UnionWith(frameworkOption.Values.Select(NuGetFramework.Parse));

                        if (string.IsNullOrEmpty(dependencyId))
                        {
                            log.LogInformation($"modifying all dependencies in {string.Join(", ", editForFrameworks.Select(e => e.GetShortFolderName()))}");
                        }
                        else
                        {
                            log.LogInformation($"modifying dependency {dependencyId} in {string.Join(", ", editForFrameworks.Select(e => e.GetShortFolderName()))}");
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(dependencyId))
                        {
                            log.LogInformation($"modifying all dependencies");
                        }
                        else
                        {
                            log.LogInformation($"modifying dependency {dependencyId} in all frameworks");
                        }
                    }

                    var version = dependencyVersionOption.HasValue() ? dependencyVersionOption.Value() : null;
                    var exclude = editExclude.HasValue() ? editExclude.Value() : null;
                    var include = editInclude.HasValue() ? editInclude.Value() : null;

                    var packages = Util.GetPackagesWithFilter(idFilter, versionFilter, excludeSymbolsFilter, highestVersionFilter, inputs.ToArray());

                    foreach (var package in packages)
                    {
                        log.LogMinimal($"processing {package}");

                        var nuspecXml = Util.GetNuspec(package);

                        DependenciesUtil.Process(nuspecXml, DependenciesUtil.EditType.Modify, editForFrameworks, dependencyId, version, exclude, include, clearExclude.HasValue(), clearInclude.HasValue(), log);

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