using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using NuGet.Test.Helpers;
using Xunit;

namespace NupkgWrench.Tests
{
    public class EmptyFolderCommandTests
    {
        [Fact]
        public async Task GivenThatIEmptyAFolderVerifyNestedFilesAreRemovedAndSiblingsKept()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var zipFile = CreatePackage(workingDir.Root, "a");

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "files", "emptyfolder", zipFile.FullName, "-p", "lib/net45" }, log);

                // Assert
                exitCode.Should().Be(0, log.GetMessages());
                GetZipEntries(zipFile.FullName).Should().Equal("a.nuspec", "lib/net45/_._", "lib/net451/c.dll");

                using (var zip = ZipFile.OpenRead(zipFile.FullName))
                {
                    zip.GetEntry("lib/net45/_._").Length.Should().Be(0);
                }
            }
        }

        [Theory]
        [InlineData("lib/net45")]
        [InlineData("lib/net45/")]
        [InlineData("/lib/net45")]
        [InlineData("lib\\net45")]
        [InlineData("lib/net45/_._")]
        [InlineData("lib\\net45\\_._")]
        public async Task GivenAFolderPathFormatVerifyThePlaceholderPathIsNormalized(string path)
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var zipFile = CreatePackage(workingDir.Root, "a");

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "files", "emptyfolder", zipFile.FullName, "-p", path }, log);

                // Assert
                exitCode.Should().Be(0, log.GetMessages());
                GetZipEntries(zipFile.FullName).Should().Equal("a.nuspec", "lib/net45/_._", "lib/net451/c.dll");
            }
        }

        [Fact]
        public async Task GivenMultiplePathsVerifyEachFolderIsEmptied()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var zipFile = CreatePackage(workingDir.Root, "a");

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "files", "emptyfolder", zipFile.FullName, "-p", "lib/net45", "-p", "ref/net45" }, log);

                // Assert
                exitCode.Should().Be(0, log.GetMessages());
                GetZipEntries(zipFile.FullName).Should().Equal("a.nuspec", "lib/net45/_._", "lib/net451/c.dll", "ref/net45/_._");
            }
        }

        [Fact]
        public async Task GivenThatIEmptyAFolderTwiceVerifyASinglePlaceholder()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var zipFile = CreatePackage(workingDir.Root, "a");

                var log = new TestLogger();

                // Act
                var firstExitCode = await Program.MainCore(new[] { "files", "emptyfolder", zipFile.FullName, "-p", "lib/net45" }, log);
                var secondExitCode = await Program.MainCore(new[] { "files", "emptyfolder", zipFile.FullName, "-p", "lib/net45" }, log);

                // Assert
                firstExitCode.Should().Be(0, log.GetMessages());
                secondExitCode.Should().Be(0, log.GetMessages());
                GetZipEntries(zipFile.FullName).Should().Equal("a.nuspec", "lib/net45/_._", "lib/net451/c.dll");
            }
        }

        [Fact]
        public async Task GivenAFolderWithMultiplePackagesVerifyAllAreUpdated()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var zipFileA = CreatePackage(workingDir.Root, "a");
                var zipFileB = CreatePackage(workingDir.Root, "b");

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "files", "emptyfolder", workingDir.Root, "-p", "lib/net45" }, log);

                // Assert
                exitCode.Should().Be(0, log.GetMessages());
                GetZipEntries(zipFileA.FullName).Should().Equal("a.nuspec", "lib/net45/_._", "lib/net451/c.dll");
                GetZipEntries(zipFileB.FullName).Should().Equal("b.nuspec", "lib/net45/_._", "lib/net451/c.dll");
            }
        }

        [Fact]
        public async Task GivenNoPathVerifyFailure()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var zipFile = CreatePackage(workingDir.Root, "a");
                var before = File.ReadAllBytes(zipFile.FullName);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "files", "emptyfolder", zipFile.FullName }, log);

                // Assert
                exitCode.Should().Be(1);
                log.GetMessages().Should().Contain("Missing required parameter --path.");
                File.ReadAllBytes(zipFile.FullName).Should().Equal(before);
            }
        }

        private static FileInfo CreatePackage(string root, string id)
        {
            var nupkg = new TestNuspec()
            {
                Id = id,
                Version = "1.0.0"
            }.CreateNupkg();

            nupkg.AddFile("lib/net45/a.dll");
            nupkg.AddFile("lib/net45/sub/b.dll");
            nupkg.AddFile("lib/net451/c.dll");

            return nupkg.Save(root);
        }

        private static List<string> GetZipEntries(string path)
        {
            using (var zip = ZipFile.OpenRead(path))
            {
                return zip.Entries.Select(e => e.FullName).OrderBy(e => e, StringComparer.Ordinal).ToList();
            }
        }
    }
}
