using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;

namespace NupkgWrench
{
    internal static class FrameworkAssembliesCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            var parentCommand = cmdApp.Command("frameworkassemblies", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);

            FrameworkAssembliesClearCommand.Register(parentCommand, log);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Modify the frameworkAssemblies section of a nuspec.";

            cmd.HelpOption(Constants.HelpOption);

            cmd.OnExecute(() =>
            {
                cmd.ShowHelp();

                return 0;
            });
        }
    }
}
