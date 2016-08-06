using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;
using NuGet.Packaging;

namespace NupkgWrench
{
    internal static class IdCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("id", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Display the package id of a nupkg.";
            cmd.HelpOption(Constants.HelpOption);

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
                    log.LogMinimal(reader.GetIdentity().Id);
                }

                return 0;
            });
        }
    }
}
