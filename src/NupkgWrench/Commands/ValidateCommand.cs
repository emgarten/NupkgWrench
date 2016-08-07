using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;


namespace NupkgWrench
{
    internal static class ValidateCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("validate", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Verify a nupkg can be read using NuGet's package reader.";

            var argRoot = cmd.Argument(
                "[root]",
                "Paths to individual packages or directories containing packages.",
                multipleValues: true);

            cmd.HelpOption(Constants.HelpOption);

            cmd.OnExecute(() =>
            {
                try
                {
                    cmd.ShowRootCommandFullNameAndVersion();

                    var inputs = argRoot.Values;

                    if (inputs.Count < 1)
                    {
                        inputs.Add(Directory.GetCurrentDirectory());
                    }

                    var packages = Util.GetPackages(inputs.ToArray());

                    foreach (var package in packages)
                    {
                        // Verify package
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
