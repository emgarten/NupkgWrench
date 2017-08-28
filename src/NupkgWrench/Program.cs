using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;
using NuGet.Versioning;

namespace NupkgWrench
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var logLevel = LogLevel.Information;

            if (Environment.GetEnvironmentVariable("NUPKGWRENCH_DEBUG") == "1")
            {
                logLevel = LogLevel.Debug;
            }

            var log = new ConsoleLogger(logLevel);

            var task = MainCore(args, log);
            return task.Result;
        }

        public static Task<int> MainCore(string[] args, ILogger log)
        {
#if DEBUG
            if (args.Contains("--debug"))
            {
                args = args.Skip(1).ToArray();
                while (!Debugger.IsAttached)
                {
                }

                Debugger.Break();
            }
#endif

            var assemblyVersion = NuGetVersion.Parse(typeof(Program).GetTypeInfo().Assembly.GetName().Version.ToString());

            var app = new CommandLineApplication
            {
                Name = "NupkgWrench",
                FullName = "nupkg wrench"
            };

            app.HelpOption(Constants.HelpOption);
            app.VersionOption("--version", assemblyVersion.ToNormalizedString());

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
            catch (AggregateException ex)
            {
                foreach (var inner in ex.InnerExceptions)
                {
                    log.LogError(inner.Message);
                    log.LogDebug(inner.ToString());
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                log.LogDebug(ex.ToString());
            }

            return Task.FromResult(exitCode);
        }
    }
}