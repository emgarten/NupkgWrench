using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using NuGet.Common;
using NuGet.Frameworks;

namespace NupkgWrench
{
    internal static class DependenciesRemoveCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("remove", cmd => Run(cmd, log));
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Remove package dependencies";

            // Filters
            var idFilter = cmd.Option(Constants.IdFilterTemplate, Constants.IdFilterDesc, CommandOptionType.SingleValue);
            var versionFilter = cmd.Option(Constants.VersionFilterTemplate, Constants.VersionFilterDesc, CommandOptionType.SingleValue);
            var excludeSymbolsFilter = cmd.Option(Constants.ExcludeSymbolsTemplate, Constants.ExcludeSymbolsDesc, CommandOptionType.NoValue);
            var highestVersionFilter = cmd.Option(Constants.HighestVersionFilterTemplate, Constants.HighestVersionFilterDesc, CommandOptionType.NoValue);

            // Command options
            var frameworkOption = cmd.Option(Constants.FrameworkOptionTemplate, Constants.FrameworkOptionDesc, CommandOptionType.MultipleValue);
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
                    var inputs = argRoot.Values;

                    if (inputs.Count < 1)
                    {
                        inputs.Add(Directory.GetCurrentDirectory());
                    }

                    var dependencyId = dependencyIdOption.HasValue() ? dependencyIdOption.Value() : null;
                    var type = dependencyIdOption.HasValue() ? DependenciesUtil.EditType.Remove : DependenciesUtil.EditType.Clear;

                    var editForFrameworks = new HashSet<NuGetFramework>();
                    if (frameworkOption.HasValue())
                    {
                        editForFrameworks.UnionWith(frameworkOption.Values.Select(NuGetFramework.Parse));

                        if (type == DependenciesUtil.EditType.Clear)
                        {
                            log.LogInformation($"removing all dependencies from {string.Join(", ", editForFrameworks.Select(e => e.GetShortFolderName()))}");
                        }
                        else
                        {
                            log.LogInformation($"removing dependency {dependencyId} from {string.Join(", ", editForFrameworks.Select(e => e.GetShortFolderName()))}");
                        }
                    }
                    else
                    {
                        if (type == DependenciesUtil.EditType.Clear)
                        {
                            log.LogInformation($"removing all dependencies");
                        }
                        else
                        {
                            log.LogInformation($"removing dependency {dependencyId} from all frameworks");
                        }
                    }

                    var packages = Util.GetPackagesWithFilter(idFilter, versionFilter, excludeSymbolsFilter, highestVersionFilter, inputs.ToArray());

                    foreach (var package in packages)
                    {
                        log.LogMinimal($"processing {package}");

                        var nuspecXml = Util.GetNuspec(package);

                        DependenciesUtil.Process(nuspecXml, type, editForFrameworks, dependencyId, null, null, null, false, false, log);

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