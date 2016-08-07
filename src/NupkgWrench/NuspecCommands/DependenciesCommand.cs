using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;

namespace NupkgWrench
{
    internal static class DependenciesCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            var parentCommand = cmdApp.Command("dependencies", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);

            // EditCommand.Register(parentCommand, log);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Modify the dependencies section of a nuspec.";

            cmd.HelpOption(Constants.HelpOption);

            cmd.OnExecute(() =>
            {
                cmd.ShowHelp();

                return 0;
            });
        }
    }
}
