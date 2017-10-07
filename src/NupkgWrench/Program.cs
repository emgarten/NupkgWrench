using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using NuGet.Common;
using NuGet.Versioning;

namespace NupkgWrench
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var logLevel = LogLevel.Information;

            if (CmdUtils.IsDebugModeEnabled())
            {
                logLevel = LogLevel.Debug;
            }

            var log = new ConsoleLogger(logLevel);

            var task = MainCore(args, log);
            return task.Result;
        }

        public static Task<int> MainCore(string[] args, ILogger log)
        {
            CmdUtils.LaunchDebuggerIfSet(ref args, log);

            var app = new CommandLineApplication
            {
                Name = "NupkgWrench",
                FullName = "nupkg wrench",
                Description = "A powertool for modifying nupkg files."
            };
            app.HelpOption(Constants.HelpOption);
            app.VersionOption("--version", (new NuGetVersion(CmdUtils.GetAssemblyVersion())).ToNormalizedString());

            NuspecCommand.Register(app, log);
            FilesCommand.Register(app, log);
            IdCommand.Register(app, log);
            VersionCommand.Register(app, log);
            ExtractCommand.Register(app, log);
            CompressCommand.Register(app, log);
            ListCommand.Register(app, log);
            UpdateFileNameCommand.Register(app, log);
            ReleaseCommand.Register(app, log);
            ValidateCommand.Register(app, log);

            app.OnExecute(() =>
            {
                app.ShowHelp();
                return 1;
            });

            var exitCode = 1;

            try
            {
                exitCode = app.Execute(args);
            }
            catch (CommandParsingException ex)
            {
                ex.Command.ShowHelp();
            }
            catch (Exception ex)
            {
                ExceptionUtils.LogException(ex, log);
            }

            return Task.FromResult(exitCode);
        }
    }
}