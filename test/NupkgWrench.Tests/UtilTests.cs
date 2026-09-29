using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using AwesomeAssertions;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Test.Helpers;
using NuGet.Versioning;
using Xunit;

namespace NupkgWrench.Tests
{
    public class UtilTests
    {
        private const string NuspecNamespace = "http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd";

        [Theory]
        [InlineData("a.1.0.0.symbols.nupkg", true)]
        [InlineData("/usr/home/a.1.0.0.symbols.nupkg", true)]
        [InlineData(".symbols.nupkg", true)]
        [InlineData("", false)]
        [InlineData("/usr/home/a.1.0.0.nupkg", false)]
        [InlineData("a.1.0.0.nupkg", false)]
        public void GivenAPathVerifySymbolPackage(string path, bool expected)
        {
            Util.IsSymbolPackage(path).Should().Be(expected);
        }

        [Theory]
        [InlineData("a", "1.0.0", "a.1.0.0")]
        [InlineData("a", "1.0", "a.1.0")]
        [InlineData("a", "1.0.0.0", "a.1.0.0.0")]
        [InlineData("a", "1.0.0+git", "a.1.0.0")]
        [InlineData("a", "1.0.0-beta.2+git", "a.1.0.0-beta.2")]
        [InlineData("a.1", "1.0.0-beta.2+git", "a.1.1.0.0-beta.2")]
        [InlineData("A", "1.0.0-BETA", "A.1.0.0-BETA")]
        public void GivenAnIdVersionVerifyNupkgName(string id, string version, string expected)
        {
            var identity = new PackageIdentity(id, NuGetVersion.Parse(version));

            // non symbol
            Util.GetNupkgName(identity, isSymbolPackage: false).Should().Be(expected + ".nupkg");

            // symbol
            Util.GetNupkgName(identity, isSymbolPackage: true).Should().Be(expected + ".symbols.nupkg");
        }

        [Fact]
        public void Util_GetPackagesWithFilter_NoFilters()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                CreatePackages(workingDir.Root);

                var log = new TestLogger();

                // Act
                var files = Util.GetPackagesWithFilter(
                    idFilter: null,
                    versionFilter: null,
                    excludeSymbols: false,
                    highestVersionFilter: false,
                    inputs: new[] { workingDir.Root });

                // Use only the names
                var names = new List<string>(files.Select(path => Path.GetFileName(path)));

                // Assert
                Assert.Equal(6, files.Count);
                Assert.Equal("a.1.0.nupkg", names[0]);
                Assert.Equal("a.1.0.symbols.nupkg", names[1]);
                Assert.Equal("b.2.0.0.0.nupkg", names[2]);
                Assert.Equal("b.2.0.0.0.symbols.nupkg", names[3]);
                Assert.Equal("c.2.0.0-beta.1.nupkg", names[4]);
                Assert.Equal("c.2.0.0-beta.2.nupkg", names[5]);
            }
        }

        [Theory]
        [InlineData("**/*.nupkg", 6)]
        [InlineData("subFolder/*.nupkg", 6)]
        [InlineData("subFolder/*.0.*.nupkg", 5)]
        [InlineData("*.nupkg", 0)]
        [InlineData("**", 6)]
        [InlineData("**/a.*", 2)]
        [InlineData("**/c.2.0.0-beta.1.nupkg", 1)]
        [InlineData("subFolder/c.2.0.0-beta.1.*", 1)]
        [InlineData("subFolder/d.2.0.0-beta.1.*", 0)]
        public void Util_GetPackagesWithFilter_GlobbingPatterns(string pattern, int count)
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var subFolder = Path.Combine(workingDir.Root, "subFolder");
                CreatePackages(subFolder);

                var log = new TestLogger();

                var input = workingDir.Root + Path.DirectorySeparatorChar + pattern;

                // Act
                var files = Util.GetPackagesWithFilter(
                    idFilter: null,
                    versionFilter: null,
                    excludeSymbols: false,
                    highestVersionFilter: false,
                    inputs: new[] { input });

                // Assert
                Assert.Equal(count, files.Count);
            }
        }

        [Theory]
        [InlineData("**/*.nupkg", "/**/*.nupkg")]
        [InlineData("subFolder/*.nupkg", "/*.nupkg")]
        [InlineData("subFolder/*.0.*.nupkg", "/*.0.*.nupkg")]
        [InlineData("*.nupkg", "/*.nupkg")]
        [InlineData("**", "/**")]
        [InlineData("**/a.*", "/**/a.*")]
        [InlineData("**/c.2.0.0-beta.1.nupkg", "/**/c.2.0.0-beta.1.nupkg")]
        [InlineData("subFolder/c.2.0.0-beta.1.*", "/c.2.0.0-beta.1.*")]
        [InlineData("subFolder/d.2.0.0-beta.1.*", "/d.2.0.0-beta.1.*")]
        public void Util_SplitGlobbingPattern(string pattern, string expected)
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var input = workingDir.Root + Path.DirectorySeparatorChar + pattern;

                // Act
                var parts = Util.SplitGlobbingPattern(input);

                // Assert
                Assert.True(expected == parts.Item2, parts.Item1.ToString() + "|" + parts.Item2);
            }
        }

        [Fact]
        public void Util_GetPackagesWithFilter_HighestVersion()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                CreatePackages(workingDir.Root);

                var log = new TestLogger();

                // Act
                var files = Util.GetPackagesWithFilter(
                    idFilter: null,
                    versionFilter: null,
                    excludeSymbols: false,
                    highestVersionFilter: true,
                    inputs: new[] { workingDir.Root });

                // Use only the names
                var names = new List<string>(files.Select(path => Path.GetFileName(path)));

                // Assert
                Assert.Equal(5, files.Count);
                Assert.Equal("a.1.0.nupkg", names[0]);
                Assert.Equal("a.1.0.symbols.nupkg", names[1]);
                Assert.Equal("b.2.0.0.0.nupkg", names[2]);
                Assert.Equal("b.2.0.0.0.symbols.nupkg", names[3]);
                Assert.Equal("c.2.0.0-beta.2.nupkg", names[4]);
            }
        }

        [Fact]
        public void Util_GetPackagesWithFilter_ExcludeSymbols()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                CreatePackages(workingDir.Root);

                var log = new TestLogger();

                // Act
                var files = Util.GetPackagesWithFilter(
                    idFilter: null,
                    versionFilter: null,
                    excludeSymbols: true,
                    highestVersionFilter: false,
                    inputs: new[] { workingDir.Root });

                // Use only the names
                var names = new List<string>(files.Select(path => Path.GetFileName(path)));

                // Assert
                Assert.Equal(4, files.Count);
                Assert.Equal("a.1.0.nupkg", names[0]);
                Assert.Equal("b.2.0.0.0.nupkg", names[1]);
                Assert.Equal("c.2.0.0-beta.1.nupkg", names[2]);
                Assert.Equal("c.2.0.0-beta.2.nupkg", names[3]);
            }
        }

        [Fact]
        public void Util_GetPackagesWithFilter_ExcludeSymbols_AndId()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                CreatePackages(workingDir.Root);

                var log = new TestLogger();

                // Act
                var files = Util.GetPackagesWithFilter(
                    idFilter: "c",
                    versionFilter: null,
                    excludeSymbols: true,
                    highestVersionFilter: false,
                    inputs: new[] { workingDir.Root });

                // Use only the names
                var names = new List<string>(files.Select(path => Path.GetFileName(path)));

                // Assert
                Assert.Equal(2, files.Count);
                Assert.Equal("c.2.0.0-beta.1.nupkg", names[0]);
                Assert.Equal("c.2.0.0-beta.2.nupkg", names[1]);
            }
        }

        [Fact]
        public void Util_GetPackagesWithFilter_IdFilter()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                CreatePackages(workingDir.Root);

                var log = new TestLogger();

                // Act
                var files = Util.GetPackagesWithFilter(
                    idFilter: "b",
                    versionFilter: null,
                    excludeSymbols: false,
                    highestVersionFilter: false,
                    inputs: new[] { workingDir.Root });

                // Use only the names
                var names = new List<string>(files.Select(path => Path.GetFileName(path)));

                // Assert
                Assert.Equal(2, files.Count);
                Assert.Equal("b.2.0.0.0.nupkg", names[0]);
                Assert.Equal("b.2.0.0.0.symbols.nupkg", names[1]);
            }
        }

        [Fact]
        public void Util_GetPackagesWithFilter_VersionFilter()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                CreatePackages(workingDir.Root);

                var log = new TestLogger();

                // Act
                var files = Util.GetPackagesWithFilter(
                    idFilter: null,
                    versionFilter: "2.0.*",
                    excludeSymbols: false,
                    highestVersionFilter: false,
                    inputs: new[] { workingDir.Root });

                // Use only the names
                var names = new List<string>(files.Select(path => Path.GetFileName(path)));

                // Assert
                Assert.Equal(4, files.Count);
                Assert.Equal("b.2.0.0.0.nupkg", names[0]);
                Assert.Equal("b.2.0.0.0.symbols.nupkg", names[1]);
                Assert.Equal("c.2.0.0-beta.1.nupkg", names[2]);
                Assert.Equal("c.2.0.0-beta.2.nupkg", names[3]);
            }
        }

        // Windows-style rooted path tests (backslash = '\\', forward slash = '/')
        [Theory]
        [InlineData(@"\foo\*.nupkg", true)]
        [InlineData(@"\\server\share\*.nupkg", true)]
        [InlineData(@"\\server\share\packages\*.nupkg", true)]
        [InlineData(@"C:\foo\*.nupkg", true)]
        [InlineData(@"D:\packages\*.nupkg", true)]
        [InlineData(@"foo\*.nupkg", false)]
        [InlineData(@"sub\folder\*.nupkg", false)]
        [InlineData(@"*.nupkg", false)]
        [InlineData("", false)]
        public void Util_IsPathRooted_Windows(string pattern, bool expected)
        {
            Util.IsPathRooted(pattern, '\\', '/').Should().Be(expected);
        }

        // Unix-style rooted path tests (forward slash = '/')
        [Theory]
        [InlineData("/foo/*.nupkg", true)]
        [InlineData("/usr/share/packages/*.nupkg", true)]
        [InlineData("foo/*.nupkg", false)]
        [InlineData("sub/folder/*.nupkg", false)]
        [InlineData("*.nupkg", false)]
        [InlineData("", false)]
        public void Util_IsPathRooted_Unix(string pattern, bool expected)
        {
            Util.IsPathRooted(pattern, '/', '/').Should().Be(expected);
        }

        [Theory]
        [InlineData("a", null, true)]
        [InlineData("a", "", true)]
        [InlineData("abc", "ABC", true)]
        [InlineData("ABC", "abc", true)]
        [InlineData("lib/net45/a.dll", "lib/*", true)]
        [InlineData("lib/net45/a.dll", "lib/*.dll", true)]
        [InlineData("lib/net45/a.dll", "*.pdb", false)]
        [InlineData("1.0.0-beta", "1.0.*", true)]
        [InlineData("a.dll", "?.dll", true)]
        [InlineData("ab.dll", "?.dll", false)]
        [InlineData("axdll", "a.dll", false)]
        [InlineData("a+b", "a+b", true)]
        [InlineData("aab", "a+b", false)]
        [InlineData("a(1)[2]$", "a(1)[2]$", true)]
        [InlineData("xa", "a", false)]
        [InlineData("ax", "a", false)]
        [InlineData(@"lib\net45\a.dll", "lib/net45/a.dll", true)]
        public void Util_IsMatch(string input, string pattern, bool expected)
        {
            Util.IsMatch(input, pattern).Should().Be(expected);
        }

        [Theory]
        [InlineData("lib/net45/a.dll", "lib/net45/a.dll")]
        [InlineData(@"lib\net45\a.dll", "lib/net45/a.dll")]
        [InlineData(@"\lib\net45\a.dll", "lib/net45/a.dll")]
        [InlineData("/lib/net45/a.dll", "lib/net45/a.dll")]
        [InlineData("//a", "a")]
        [InlineData("lib/net45/", "lib/net45/")]
        public void Util_GetZipPath(string path, string expected)
        {
            Util.GetZipPath(path).Should().Be(expected);
        }

        [Theory]
        [InlineData("a/b.dll", "a/b")]
        [InlineData("a/b.c.pdb", "a/b.c")]
        [InlineData("/usr/home/blah.txt", "/usr/home/blah")]
        [InlineData("a/noext", "a/noext")]
        [InlineData("a.b/noext", "a.b/noext")]
        [InlineData(".pdb", ".pdb")]
        public void Util_RemoveFileExtensionFromPath(string path, string expected)
        {
            Util.RemoveFileExtensionFromPath(path).Should().Be(expected);
        }

        [Fact]
        public void Util_AddOrUpdateMetadataElement_UpdatesExistingElement()
        {
            // Arrange
            var doc = CreateNuspec("<releaseNotes>old</releaseNotes>");

            // Act
            Util.AddOrUpdateMetadataElement(doc, "RELEASENOTES", "new");

            // Assert
            var element = Util.GetMetadataElement(doc).Elements().Single(e => e.Name.LocalName.Equals("releaseNotes", StringComparison.OrdinalIgnoreCase));
            element.Name.Should().Be(XName.Get("releaseNotes", NuspecNamespace));
            element.Value.Should().Be("new");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Util_AddOrUpdateMetadataElement_RemovesExistingElementWhenValueIsEmpty(string value)
        {
            // Arrange
            var doc = CreateNuspec("<releaseNotes>old</releaseNotes>");

            // Act
            Util.AddOrUpdateMetadataElement(doc, "releaseNotes", value);

            // Assert
            Util.GetMetadataElement(doc).Elements().Select(e => e.Name.LocalName).Should().BeEquivalentTo(new[] { "id", "version" });
        }

        [Fact]
        public void Util_AddOrUpdateMetadataElement_AddsMissingElement()
        {
            // Arrange
            var doc = CreateNuspec(string.Empty);

            // Act
            Util.AddOrUpdateMetadataElement(doc, "title", "My title");

            // Assert
            Util.GetMetadataElement(doc).Element(XName.Get("title", NuspecNamespace)).Value.Should().Be("My title");
            new NuspecReader(doc).GetTitle().Should().Be("My title");
        }

        [Fact]
        public void Util_AddOrUpdateMetadataElement_ThrowsWhenMetadataIsMissing()
        {
            // Arrange
            var doc = XDocument.Parse($"<package xmlns=\"{NuspecNamespace}\" />");

            // Act & Assert
            Assert.Throws<InvalidDataException>(() => Util.AddOrUpdateMetadataElement(doc, "title", "My title"));
        }

        [Fact]
        public void Util_GetPackagesWithFilter_MissingNupkgThrows()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var path = Path.Combine(workingDir.Root, "missing.1.0.0.nupkg");

                // Act
                var exception = Assert.Throws<FileNotFoundException>(() => Util.GetPackagesWithFilter(
                    idFilter: null,
                    versionFilter: null,
                    excludeSymbols: false,
                    highestVersionFilter: false,
                    inputs: new[] { path }));

                // Assert
                exception.Message.Should().StartWith("Unable to find").And.Contain("missing.1.0.0.nupkg");
            }
        }

        [Fact]
        public void Util_GetPackagesWithFilter_MissingFolderAndNonNupkgFilesAreIgnored()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var textFile = Path.Combine(workingDir.Root, "readme.txt");
                File.WriteAllText(textFile, "a");

                var inputs = new[]
                {
                    Path.Combine(workingDir.Root, "missing"),
                    Path.Combine(workingDir.Root, "missing.txt"),
                    textFile
                };

                // Act
                var files = Util.GetPackagesWithFilter(
                    idFilter: null,
                    versionFilter: null,
                    excludeSymbols: false,
                    highestVersionFilter: false,
                    inputs: inputs);

                // Assert
                files.Should().BeEmpty();
            }
        }

        [Fact]
        public void Util_GetPackagesWithFilter_DuplicateInputsAreRemoved()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var zipFile = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0.0"
                }.CreateNupkg().Save(workingDir.Root);

                // Act
                var files = Util.GetPackagesWithFilter(
                    idFilter: null,
                    versionFilter: null,
                    excludeSymbols: false,
                    highestVersionFilter: false,
                    inputs: new[] { workingDir.Root, zipFile.FullName, zipFile.FullName });

                // Assert
                files.Should().Equal(zipFile.FullName);
            }
        }

        private static XDocument CreateNuspec(string extraMetadata)
        {
            return XDocument.Parse($"<package xmlns=\"{NuspecNamespace}\"><metadata><id>a</id><version>1.0.0</version>{extraMetadata}</metadata></package>");
        }

        private static void CreatePackages(string workingDir)
        {
            var testPackageA = new TestNupkg()
            {
                Nuspec = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0"
                }
            };

            var testPackageB = new TestNupkg()
            {
                Nuspec = new TestNuspec()
                {
                    Id = "b",
                    Version = "2.0.0.0"
                }
            };

            var testPackageC1 = new TestNupkg()
            {
                Nuspec = new TestNuspec()
                {
                    Id = "c",
                    Version = "2.0.0-beta.1"
                }
            };

            var testPackageC2 = new TestNupkg()
            {
                Nuspec = new TestNuspec()
                {
                    Id = "c",
                    Version = "2.0.0-beta.2"
                }
            };

            var zipFileA = testPackageA.Save(workingDir);
            var zipFileB = testPackageB.Save(workingDir);
            var zipFileC1 = testPackageC1.Save(workingDir);
            var zipFileC2 = testPackageC2.Save(workingDir);
            zipFileA.CopyTo(zipFileA.FullName.Replace(".nupkg", ".symbols.nupkg"));
            zipFileB.CopyTo(zipFileB.FullName.Replace(".nupkg", ".symbols.nupkg"));
        }
    }
}