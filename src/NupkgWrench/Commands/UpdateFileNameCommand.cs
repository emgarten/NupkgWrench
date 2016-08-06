using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Core;

namespace NupkgWrench
{
    internal static class UpdateFileNameCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("updatefilename", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Update the file name of a package to match the id and version in the nuspec.";
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

                PackageIdentity identity = null;

                using (var reader = new PackageArchiveReader(nupkgPath))
                {
                    identity = reader.GetIdentity();
                }

                var expectedName = $"{identity.Id}.{identity.Version.ToString()}.nupkg";
                var currentName = Path.GetFileName(nupkgPath);

                var dir = Path.GetDirectoryName(nupkgPath);
                var expectedPath = Path.Combine(dir, expectedName);

                if (StringComparer.Ordinal.Equals(expectedName, currentName))
                {
                    log.LogMinimal($"{nupkgPath} : no changes");
                }
                else
                {
                    if (File.Exists(expectedPath))
                    {
                        log.LogMinimal($"removing : {expectedPath}");
                        File.Delete(expectedPath);
                    }

                    log.LogMinimal($"{nupkgPath} : -> {expectedName}");
                    File.Move(nupkgPath, expectedPath);
                }

                return 0;
            });
        }
    }
}
