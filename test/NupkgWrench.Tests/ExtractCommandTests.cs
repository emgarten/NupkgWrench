using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using AwesomeAssertions;
using NuGet.Test.Helpers;
using Xunit;

namespace NupkgWrench.Tests
{
    public class ExtractCommandTests
    {
        [Fact]
        public async Task GivenThatIExtractANupkgVerifyAllFilesAreWritten()
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
                nupkg.AddFile("lib/net45/sub/b.dll");
                nupkg.AddFile("content/readme.txt");

                var zipFile = nupkg.Save(workingDir.Root);
                var outputDir = Path.Combine(workingDir.Root, "output");

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "extract", zipFile.FullName, "-o", outputDir }, log);

                // Assert
                exitCode.Should().Be(0, log.GetMessages());

                var files = GetFolderFiles(outputDir);
                files.Keys.Should().BeEquivalentTo(new[] { "a.nuspec", "content/readme.txt", "lib/net45/a.dll", "lib/net45/sub/b.dll" });
                files.Should().BeEquivalentTo(GetZipEntries(zipFile.FullName), options => options.WithStrictOrdering());
            }
        }

        [Fact]
        public async Task GivenThatIExtractANupkgWithDirectoryEntriesVerifyAllFilesAndFoldersAreWritten()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var zipPath = Path.Combine(workingDir.Root, "a.1.0.0.nupkg");

                using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    using (var entryStream = zip.CreateEntry("a.nuspec").Open())
                    {
                        new TestNuspec()
                        {
                            Id = "a",
                            Version = "1.0.0"
                        }.Create().Save(entryStream);
                    }

                    zip.CreateEntry("lib/");
                    zip.CreateEntry("lib/net45/");

                    using (var entryStream = zip.CreateEntry("lib/net45/a.dll").Open())
                    {
                        entryStream.Write(new byte[] { 1, 2, 3 });
                    }

                    zip.CreateEntry("empty/");
                }

                var outputDir = Path.Combine(workingDir.Root, "output");

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "extract", zipPath, "-o", outputDir }, log);

                // Assert
                exitCode.Should().Be(0, log.GetMessages());

                var files = GetFolderFiles(outputDir);
                files.Keys.Should().BeEquivalentTo(new[] { "a.nuspec", "lib/net45/a.dll" });
                files["lib/net45/a.dll"].Should().Equal(1, 2, 3);
                Directory.Exists(Path.Combine(outputDir, "empty")).Should().BeTrue();
            }
        }

        [Fact]
        public async Task GivenThatIExtractWithoutAnOutputFolderVerifyFailure()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var zipFile = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0.0"
                }.CreateNupkg().Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "extract", zipFile.FullName }, log);

                // Assert
                exitCode.Should().Be(1);
                log.GetMessages().Should().Contain("Missing required parameter --output.");
            }
        }

        [Fact]
        public async Task GivenThatIExtractAFolderWithMultiplePackagesVerifyFailure()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var packagesDir = Path.Combine(workingDir.Root, "packages");
                var outputDir = Path.Combine(workingDir.Root, "output");
                CreatePackages(packagesDir);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "extract", packagesDir, "-o", outputDir }, log);

                // Assert
                exitCode.Should().Be(1);
                log.GetMessages().Should().Contain("match multiple nupkgs");
                Directory.Exists(outputDir).Should().BeFalse();
            }
        }

        [Fact]
        public async Task GivenThatIExtractAFolderWithAnIdFilterVerifyOnlyThatPackageIsExtracted()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var packagesDir = Path.Combine(workingDir.Root, "packages");
                var outputDir = Path.Combine(workingDir.Root, "output");
                CreatePackages(packagesDir);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "extract", packagesDir, "--id", "b", "-o", outputDir }, log);

                // Assert
                exitCode.Should().Be(0, log.GetMessages());
                GetFolderFiles(outputDir).Keys.Should().BeEquivalentTo(new[] { "b.nuspec" });
            }
        }

        private static void CreatePackages(string root)
        {
            Directory.CreateDirectory(root);

            new TestNuspec()
            {
                Id = "a",
                Version = "1.0.0"
            }.CreateNupkg().Save(root);

            new TestNuspec()
            {
                Id = "b",
                Version = "1.0.0"
            }.CreateNupkg().Save(root);
        }

        private static SortedDictionary<string, byte[]> GetFolderFiles(string root)
        {
            var files = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);

            foreach (var path in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                files.Add(Path.GetRelativePath(root, path).Replace('\\', '/'), File.ReadAllBytes(path));
            }

            return files;
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
