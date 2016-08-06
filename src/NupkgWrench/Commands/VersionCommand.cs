using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;
using NuGet.Packaging;

namespace NupkgWrench
{
    internal static class VersionCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("version", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Display the package version of a nupkg.";
            cmd.HelpOption(Constants.HelpOption);
            var normalizeOption = cmd.Option("-n|--normalize", "Normalize the version to remove leading and trailing zeros.", CommandOptionType.NoValue);

            var argRoot = cmd.Argument(
                "[root]",
                "Nupkg path",
                multipleValues: false);

            cmd.OnExecute(() =>
            {
                var nupkgPath = argRoot.Value;

                if (string.IsNullOrEmpty(nupkgPath))
                {
                    throw new ArgumentException("Specify the path to a nupkg.");
                }

                using (var reader = new PackageArchiveReader(nupkgPath))
                {
                    var version = reader.GetIdentity().Version;

                    if (version.IsSemVer2 || normalizeOption.HasValue())
                    {
                        log.LogMinimal(version.ToFullString());
                    }
                    else
                    {
                        log.LogMinimal(version.ToString());
                    }
                }

                return 0;
            });
        }
    }
}
