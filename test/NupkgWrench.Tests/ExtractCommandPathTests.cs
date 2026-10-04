using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AwesomeAssertions;
using NuGet.Common;
using NuGet.Test.Helpers;
using Test.Common;
using Xunit;

namespace NupkgWrench.Tests
{
    public class ExtractCommandPathTests
    {
        [Theory]
        [InlineData("../evil.txt")]
        [InlineData("lib/../../evil.txt")]
        [InlineData("../out2/evil.txt")]
        // OUT and out are different folders when the parent folder is case sensitive.
        [InlineData("../OUT/evil.txt")]
        public async Task GivenThatAnEntryIsOutsideTheOutputFolderVerifyNothingIsExtracted(string entryName)
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var log = new TestLogger();
                var outputDir = Path.Combine(workingDir.Root, "out");
                var path = CreateNupkg(workingDir, "lib/net45/a.dll", entryName);

                // Act
                var exitCode = await Program.MainCore(new[] { "extract", path, "-o", outputDir }, log);

                // Assert
                VerifyEntryRejected(workingDir, path, entryName, exitCode, log);
            }
        }

        [Theory]
        [InlineData(@"..\evil.txt")]
        [InlineData(@"lib\..\..\evil.txt")]
        public async Task GivenThatAnEntryContainsBackslashTraversalVerifyItIsNotExtractedOutsideTheOutputFolder(string entryName)
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var log = new TestLogger();
                var outputDir = Path.Combine(workingDir.Root, "out");
                var path = CreateNupkg(workingDir, "lib/net45/a.dll", entryName);

                // Act
                var exitCode = await Program.MainCore(new[] { "extract", path, "-o", outputDir }, log);

                // Assert
                if (RuntimeEnvironmentHelper.IsWindows)
                {
                    // Windows treats \ as a directory separator.
                    VerifyEntryRejected(workingDir, path, entryName, exitCode, log);
                }
                else
                {
                    // \ is a valid file name character on other platforms.
                    exitCode.Should().Be(0, log.GetMessages());
                    File.Exists(Path.Combine(outputDir, entryName)).Should().BeTrue();
                    File.Exists(Path.Combine(workingDir.Root, "evil.txt")).Should().BeFalse();
                }
            }
        }

        [Theory]
        [InlineData("evil.txt")]
        [InlineData("out/evil.txt")]
        [InlineData("OUT/evil.txt")]
        public async Task GivenThatAnEntryIsRootedVerifyNothingIsExtracted(string target)
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var log = new TestLogger();
                var outputDir = Path.Combine(workingDir.Root, "out");
                var entryName = Path.Combine(workingDir.Root, target).Replace('\\', '/');
                var path = CreateNupkg(workingDir, "lib/net45/a.dll", entryName);

                // Act
                var exitCode = await Program.MainCore(new[] { "extract", path, "-o", outputDir }, log);

                // Assert
                VerifyEntryRejected(workingDir, path, entryName, exitCode, log);
            }
        }

        [WindowsTheory]
        [InlineData("evil.txt")]
        [InlineData("out/evil.txt")]
        [InlineData("OUT/evil.txt")]
        public async Task GivenThatAnEntryIsRootedWithoutADriveVerifyNothingIsExtracted(string target)
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var log = new TestLogger();
                var outputDir = Path.Combine(workingDir.Root, "out");
                var evilPath = Path.Combine(workingDir.Root, target);
                var entryName = "/" + Path.GetRelativePath(Path.GetPathRoot(evilPath), evilPath).Replace('\\', '/');
                var path = CreateNupkg(workingDir, "lib/net45/a.dll", entryName);

                // Act
                var exitCode = await Program.MainCore(new[] { "extract", path, "-o", outputDir }, log);

                // Assert
                VerifyEntryRejected(workingDir, path, entryName, exitCode, log);
            }
        }

        [Theory]
        [InlineData("./a.txt")]
        [InlineData("lib/../a.txt")]
        public async Task GivenThatAnEntryHasDotSegmentsInsideTheOutputFolderVerifyItIsExtracted(string entryName)
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var log = new TestLogger();
                var outputDir = Path.Combine(workingDir.Root, "out");
                var path = CreateNupkg(workingDir, entryName);

                // Act
                var exitCode = await Program.MainCore(new[] { "extract", path, "-o", outputDir }, log);

                // Assert
                exitCode.Should().Be(0, log.GetMessages());
                GetFiles(outputDir).Should().BeEquivalentTo(new[] { "a.nuspec", "a.txt" });
            }
        }

        [Theory]
        [InlineData("./")]
        [InlineData("lib/../")]
        public async Task GivenThatADirectoryEntryIsTheOutputFolderVerifyAllFilesAreExtracted(string entryName)
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var log = new TestLogger();
                var outputDir = Path.Combine(workingDir.Root, "out");
                var path = CreateNupkg(workingDir, entryName, "lib/net45/a.dll");

                // Act
                var exitCode = await Program.MainCore(new[] { "extract", path, "-o", outputDir }, log);

                // Assert
                exitCode.Should().Be(0, log.GetMessages());
                GetFiles(outputDir).Should().BeEquivalentTo(new[] { "a.nuspec", Path.Combine("lib", "net45", "a.dll") });
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GivenThatAPackageIsValidVerifyAllEntriesAreExtracted(bool trailingSeparator)
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var log = new TestLogger();
                var outputDir = Path.Combine(workingDir.Root, "out");
                var nupkg = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0.0"
                }.CreateNupkg();
                nupkg.Files.Clear();
                nupkg.AddFile("lib/net45/a.dll", new byte[] { 1, 2, 3 });
                nupkg.AddFile("content/a..b.txt", Encoding.UTF8.GetBytes("a..b"));
                var path = nupkg.Save(workingDir).FullName;
                var outputArg = trailingSeparator ? outputDir + Path.DirectorySeparatorChar : outputDir;

                // Act
                var exitCode = await Program.MainCore(new[] { "extract", path, "-o", outputArg }, log);

                // Assert
                exitCode.Should().Be(0, log.GetMessages());
                GetFiles(outputDir).Should().BeEquivalentTo(new[]
                {
                    "a.nuspec",
                    Path.Combine("lib", "net45", "a.dll"),
                    Path.Combine("content", "a..b.txt")
                });

                using (var zip = ZipFile.OpenRead(path))
                {
                    foreach (var entry in zip.Entries)
                    {
                        File.ReadAllBytes(Path.Combine(outputDir, entry.FullName)).Should().Equal(ReadEntry(entry), entry.FullName);
                    }
                }
            }
        }

        private static string CreateNupkg(string root, params string[] entryNames)
        {
            var nupkg = new TestNuspec()
            {
                Id = "a",
                Version = "1.0.0"
            }.CreateNupkg();
            nupkg.Files.Clear();

            foreach (var entryName in entryNames)
            {
                nupkg.AddFile(entryName);
            }

            return nupkg.Save(root).FullName;
        }

        private static void VerifyEntryRejected(string root, string nupkgPath, string entryName, int exitCode, TestLogger log)
        {
            exitCode.Should().Be(1, log.GetMessages());
            log.GetMessages(LogLevel.Error).Should().Contain($"Package entry '{entryName}' is not a relative path inside the output folder");

            // Nothing is written for the package, not even the output folder.
            Directory.GetFileSystemEntries(root, "*", SearchOption.AllDirectories)
                .Select(e => Path.GetRelativePath(root, e))
                .Should().BeEquivalentTo(new[] { Path.GetFileName(nupkgPath) });
        }

        private static string[] GetFiles(string root)
        {
            return Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                .Select(e => Path.GetRelativePath(root, e))
                .ToArray();
        }

        private static byte[] ReadEntry(ZipArchiveEntry entry)
        {
            using (var stream = entry.Open())
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
