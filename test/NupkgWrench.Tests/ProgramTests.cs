using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using NuGet.Common;
using NuGet.Test.Helpers;
using Xunit;

namespace NupkgWrench.Tests
{
    public class ProgramTests
    {
        [Theory]
        [InlineData("", 1)]
        [InlineData("foo", 1)]
        [InlineData("list --foo", 1)]
        [InlineData("--help", 0)]
        [InlineData("--version", 0)]
        [InlineData("nuspec", 0)]
        [InlineData("files", 0)]
        [InlineData("nuspec dependencies", 0)]
        [InlineData("nuspec contentfiles", 0)]
        [InlineData("nuspec frameworkassemblies", 0)]
        public async Task GivenCommandLineArgumentsVerifyTheExitCode(string args, int expectedExitCode)
        {
            // Arrange
            var log = new TestLogger();

            // Act
            var exitCode = await Program.MainCore(args.Split(' ', StringSplitOptions.RemoveEmptyEntries), log);

            // Assert
            exitCode.Should().Be(expectedExitCode, log.GetMessages());
        }

        [Theory]
        [InlineData("id")]
        [InlineData("version")]
        [InlineData("list")]
        [InlineData("validate")]
        [InlineData("updatefilename")]
        [InlineData("release")]
        [InlineData("extract -o {output}")]
        [InlineData("files list")]
        [InlineData("files add -p lib/net45/a.dll -f {file}")]
        [InlineData("files remove -p lib/net45/a.dll")]
        [InlineData("files emptyfolder -p lib/net45")]
        [InlineData("files copysymbols")]
        [InlineData("nuspec show")]
        [InlineData("nuspec edit -p title -s a")]
        [InlineData("nuspec contentfiles add --include **/*")]
        [InlineData("nuspec dependencies add --dependency-id b --dependency-version 1.0.0")]
        [InlineData("nuspec dependencies modify --dependency-version 1.0.0")]
        [InlineData("nuspec dependencies remove")]
        [InlineData("nuspec dependencies clear")]
        [InlineData("nuspec dependencies emptygroup -f net45")]
        [InlineData("nuspec frameworkassemblies add -n System")]
        [InlineData("nuspec frameworkassemblies clear")]
        public async Task GivenAMissingNupkgVerifyTheCommandFails(string command)
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var inputFile = Path.Combine(workingDir.Root, "a.dll");
                File.WriteAllText(inputFile, "a");

                var outputDir = Path.Combine(workingDir.Root, "output");
                var missingPath = Path.Combine(workingDir.Root, "missing.1.0.0.nupkg");

                var args = command.Split(' ')
                    .Select(e => e switch
                    {
                        "{file}" => inputFile,
                        "{output}" => outputDir,
                        _ => e
                    })
                    .Append(missingPath)
                    .ToArray();

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(args, log);

                // Assert
                exitCode.Should().Be(1);
                string.Join("|", log.Messages.Where(e => e.Level == LogLevel.Error).Select(e => e.Message)).Should().Contain("Unable to find");
                File.Exists(missingPath).Should().BeFalse();
                Directory.Exists(outputDir).Should().BeFalse();
            }
        }
    }
}
