using System;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;
using NuGet.Packaging;

namespace NupkgWrench
{
    internal static class ShowCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("show", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Display the XML contents of a nuspec file from a package.";
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
                using (var streamReader = new StreamReader(reader.GetNuspec()))
                {
                    log.LogMinimal(streamReader.ReadToEnd());
                }

                return 0;
            });
        }
    }
}
