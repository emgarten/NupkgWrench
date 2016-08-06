using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;


namespace NupkgWrench
{
    internal static class TransformCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("transform", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Apply an XML transform to the nuspec.";

            var idFilter = cmd.Option("-i|--id", "Filter to only packages matching the id or wildcard.", CommandOptionType.SingleValue);
            var versionFilter = cmd.Option("-v|--version", "Filter to only packages matching the version or wildcard.", CommandOptionType.SingleValue);
            var xsltPath = cmd.Option("-x|--xslt", "XSLT file path.", CommandOptionType.SingleValue);

            var argRoot = cmd.Argument(
                "[root]",
                "Paths to individual packages or directories containing packages.",
                multipleValues: true);

            cmd.HelpOption(Constants.HelpOption);

            var required = new List<CommandOption>()
            {
                xsltPath
            };

            cmd.OnExecute(() =>
            {
                try
                {
                    cmd.ShowRootCommandFullNameAndVersion();

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

                    var packages = Util.GetPackages(inputs);

                    foreach (var package in packages)
                    {
                        // Run transform
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
