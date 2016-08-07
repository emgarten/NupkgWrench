using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;
using NuGet.Packaging;

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
                    var inputs = argRoot.Values;

                    if (inputs.Count < 1)
                    {
                        inputs.Add(Directory.GetCurrentDirectory());
                    }

                    var packages = Util.GetPackages(inputs.ToArray());
                    var exitCode = 0;

                    foreach (var package in packages)
                    {
                        // Verify package
                        try
                        {
                            using (var reader = new PackageArchiveReader(package))
                            {
                                var identity = reader.GetIdentity();

                                log.LogMinimal($"valid : {package}");
                            }
                        }
                        catch (Exception ex)
                        {
                            log.LogMinimal($"error : {package} : {ex.Message}");
                            exitCode = 1;
                        }
                    }

                    return exitCode;
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
