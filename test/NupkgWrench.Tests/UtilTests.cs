using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace NupkgWrench.Tests
{
    public class UtilTests
    {
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

        private static void CreatePackages(string workingDir)
        {
            var testPackageA = new TestPackageContext()
            {
                Nuspec = new TestNuspecContext()
                {
                    Id = "a",
                    Version = "1.0"
                }
            };

            var testPackageB = new TestPackageContext()
            {
                Nuspec = new TestNuspecContext()
                {
                    Id = "b",
                    Version = "2.0.0.0"
                }
            };

            var testPackageC1 = new TestPackageContext()
            {
                Nuspec = new TestNuspecContext()
                {
                    Id = "c",
                    Version = "2.0.0-beta.1"
                }
            };

            var testPackageC2 = new TestPackageContext()
            {
                Nuspec = new TestNuspecContext()
                {
                    Id = "c",
                    Version = "2.0.0-beta.2"
                }
            };

            var zipFileA = testPackageA.Create(workingDir);
            var zipFileB = testPackageB.Create(workingDir);
            var zipFileC1 = testPackageC1.Create(workingDir);
            var zipFileC2 = testPackageC2.Create(workingDir);
            zipFileA.CopyTo(zipFileA.FullName.Replace(".nupkg", ".symbols.nupkg"));
            zipFileB.CopyTo(zipFileB.FullName.Replace(".nupkg", ".symbols.nupkg"));
        }
    }
}