using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using AwesomeAssertions;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Test.Helpers;
using NuGet.Versioning;
using Xunit;

namespace NupkgWrench.Tests
{
    public class CompressCommandTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GivenThatICompressAFolderVerifyTheNupkgIsCreated(bool trailingSeparator)
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var inputDir = Path.Combine(workingDir.Root, "input");
                var outputDir = Path.Combine(workingDir.Root, "output");
                CreatePackageFolder(inputDir, "a", "1.0.0-beta");

                var input = trailingSeparator ? inputDir + Path.DirectorySeparatorChar : inputDir;

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "compress", input, "-o", outputDir }, log);

                // Assert
                exitCode.Should().Be(0, log.GetMessages());

                var nupkgPath = Path.Combine(outputDir, "a.1.0.0-beta.nupkg");
                Directory.GetFiles(outputDir).Should().BeEquivalentTo(new[] { nupkgPath });
                GetZipEntries(nupkgPath).Keys.Should().BeEquivalentTo(new[] { "a.nuspec", "lib/net45/a.dll" });

                using (var reader = new PackageArchiveReader(nupkgPath))
                {
                    reader.GetIdentity().Should().Be(new PackageIdentity("a", NuGetVersion.Parse("1.0.0-beta")));
                }
            }
        }

        [Fact]
        public async Task GivenThatIExtractAndCompressANupkgVerifyTheContentIsUnchanged()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var nupkg = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0.0"
                }.CreateNupkg();

                nupkg.AddFile("lib/net45/a.dll", new byte[] { 1, 2, 3 });
                nupkg.AddFile("build/a.targets");

                var zipFile = nupkg.Save(workingDir.Root);
                var extractDir = Path.Combine(workingDir.Root, "extract");
                var outputDir = Path.Combine(workingDir.Root, "output");

                var log = new TestLogger();

                // Act
                var extractExitCode = await Program.MainCore(new[] { "extract", zipFile.FullName, "-o", extractDir }, log);
                var compressExitCode = await Program.MainCore(new[] { "compress", extractDir, "-o", outputDir }, log);

                // Assert
                extractExitCode.Should().Be(0, log.GetMessages());
                compressExitCode.Should().Be(0, log.GetMessages());
                GetZipEntries(Path.Combine(outputDir, zipFile.Name)).Should().BeEquivalentTo(GetZipEntries(zipFile.FullName), options => options.WithStrictOrdering());
            }
        }

        [Fact]
        public async Task GivenThatICompressWithoutAnOutputFolderVerifyFailure()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var inputDir = Path.Combine(workingDir.Root, "input");
                CreatePackageFolder(inputDir, "a", "1.0.0");

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "compress", inputDir }, log);

                // Assert
                exitCode.Should().Be(1);
                log.GetMessages().Should().Contain("Missing required parameter --output.");
            }
        }

        [Fact]
        public async Task GivenThatICompressWithoutAFolderVerifyFailure()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var outputDir = Path.Combine(workingDir.Root, "output");

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "compress", "-o", outputDir }, log);

                // Assert
                exitCode.Should().Be(1);
                log.GetMessages().Should().Contain("Specify the path to a folder containing nupkg files.");
                log.GetMessages().Should().NotContain("NullReferenceException");
                Directory.Exists(outputDir).Should().BeFalse();
            }
        }

        [Fact]
        public async Task GivenThatICompressAMissingFolderVerifyFailure()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var inputDir = Path.Combine(workingDir.Root, "missing");
                var outputDir = Path.Combine(workingDir.Root, "output");

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "compress", inputDir, "-o", outputDir }, log);

                // Assert
                exitCode.Should().Be(1);
                log.GetMessages().Should().Contain("Specify the path to a folder");
                Directory.Exists(outputDir).Should().BeFalse();
            }
        }

        private static void CreatePackageFolder(string root, string id, string version)
        {
            Directory.CreateDirectory(Path.Combine(root, "lib", "net45"));

            new TestNuspec()
            {
                Id = id,
                Version = version
            }.Create().Save(Path.Combine(root, $"{id}.nuspec"));

            File.WriteAllText(Path.Combine(root, "lib", "net45", $"{id}.dll"), id);
        }

        private static SortedDictionary<string, byte[]> GetZipEntries(string path)
        {
            var entries = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);

            using (var zip = ZipFile.OpenRead(path))
            {
                foreach (var entry in zip.Entries)
                {
                    using (var entryStream = entry.Open())
                    using (var memoryStream = new MemoryStream())
                    {
                        entryStream.CopyTo(memoryStream);
                        entries.Add(entry.FullName, memoryStream.ToArray());
                    }
                }
            }

            return entries;
        }
    }
}
