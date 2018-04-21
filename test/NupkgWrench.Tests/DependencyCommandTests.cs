using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Test.Helpers;
using NuGet.Versioning;
using Xunit;

namespace NupkgWrench.Tests
{
    public class DependencyCommandTests
    {
        [Fact]
        public async Task DependencyCommandTests_AddVerifyAddWithMultipleGroups()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackageA = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var depGroup1 = new PackageDependencyGroup(NuGetFramework.Parse("net45"), new[] {
                    new PackageDependency("b", VersionRange.Parse("1.0.0"))
                });

                var depGroup2 = new PackageDependencyGroup(NuGetFramework.Parse("net46"), new[] {
                    new PackageDependency("b", VersionRange.Parse("2.0.0"))
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup1);
                testPackageA.Nuspec.Dependencies.Add(depGroup2);

                var zipFileA = testPackageA.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "add", workingDir.Root, "--dependency-id", "c", "--dependency-version", "1.0.0" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.1.0.0.nupkg"));
                var dependencyCNet45 = nuspecA.GetDependencyGroups().Single(e => e.TargetFramework == NuGetFramework.Parse("net45")).Packages.Single(e => e.Id == "c");
                var dependencyCNet46 = nuspecA.GetDependencyGroups().Single(e => e.TargetFramework == NuGetFramework.Parse("net46")).Packages.Single(e => e.Id == "c");

                // Assert
                dependencyCNet45.VersionRange.Should().Be(VersionRange.Parse("1.0.0"));
                dependencyCNet46.VersionRange.Should().Be(VersionRange.Parse("1.0.0"));
                nuspecA.GetDependencyGroups().Count().Should().Be(2);
            }
        }

        [Fact]
        public async Task DependencyCommandTests_AddVerifyAddWithFramework()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackageA = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var depGroup1 = new PackageDependencyGroup(NuGetFramework.Parse("net45"), new[] {
                    new PackageDependency("b", VersionRange.Parse("1.0.0"))
                });

                var depGroup2 = new PackageDependencyGroup(NuGetFramework.Parse("net46"), new[] {
                    new PackageDependency("b", VersionRange.Parse("2.0.0"))
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup1);
                testPackageA.Nuspec.Dependencies.Add(depGroup2);

                var zipFileA = testPackageA.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "add", workingDir.Root, "--dependency-id", "c", "--dependency-version", "1.0.0", "--framework", "net4.5" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.1.0.0.nupkg"));
                var dependencyCNet45 = nuspecA.GetDependencyGroups().Single(e => e.TargetFramework == NuGetFramework.Parse("net45")).Packages.Single(e => e.Id == "c");
                var dependencyCNet46 = nuspecA.GetDependencyGroups().Single(e => e.TargetFramework == NuGetFramework.Parse("net46")).Packages.FirstOrDefault(e => e.Id == "c");

                // Assert
                dependencyCNet45.VersionRange.Should().Be(VersionRange.Parse("1.0.0"));
                dependencyCNet46.Should().BeNull();
                nuspecA.GetDependencyGroups().Count().Should().Be(2);
            }
        }

        [Fact]
        public async Task DependencyCommandTests_AddVerifyAddWithNewFramework()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackageA = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var depGroup1 = new PackageDependencyGroup(NuGetFramework.Parse("net45"), new[] {
                    new PackageDependency("b", VersionRange.Parse("1.0.0"))
                });

                var depGroup2 = new PackageDependencyGroup(NuGetFramework.Parse("net46"), new[] {
                    new PackageDependency("b", VersionRange.Parse("2.0.0"))
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup1);
                testPackageA.Nuspec.Dependencies.Add(depGroup2);

                var zipFileA = testPackageA.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "add", workingDir.Root, "--dependency-id", "c", "--dependency-version", "1.0.0", "--framework", "any" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.1.0.0.nupkg"));
                var groups = nuspecA.GetDependencyGroups();

                var dependencyCAny = groups.Single(e => e.TargetFramework.IsAny).Packages.FirstOrDefault(e => e.Id == "c");
                var dependencyCNet45 = groups.Single(e => e.TargetFramework == NuGetFramework.Parse("net45")).Packages.FirstOrDefault(e => e.Id == "c");
                var dependencyCNet46 = groups.Single(e => e.TargetFramework == NuGetFramework.Parse("net46")).Packages.FirstOrDefault(e => e.Id == "c");

                // Assert
                dependencyCNet45.Should().BeNull();
                dependencyCNet46.Should().BeNull();
                dependencyCAny.VersionRange.Should().Be(VersionRange.Parse("1.0.0"));
                nuspecA.GetDependencyGroups().Count().Should().Be(3);
            }
        }

        [Fact]
        public async Task DependencyCommandTests_AddVerifyAddWithNewVersion()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackageA = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var depGroup1 = new PackageDependencyGroup(NuGetFramework.Parse("net45"), new[] {
                    new PackageDependency("b", VersionRange.Parse("1.0.0"))
                });

                var depGroup2 = new PackageDependencyGroup(NuGetFramework.Parse("net46"), new[] {
                    new PackageDependency("b", VersionRange.Parse("2.0.0"))
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup1);
                testPackageA.Nuspec.Dependencies.Add(depGroup2);

                var zipFileA = testPackageA.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "add", workingDir.Root, "--dependency-id", "c", "--dependency-version", "[3.0.0]" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.1.0.0.nupkg"));
                var groups = nuspecA.GetDependencyGroups();

                var dependencyCNet45 = groups.Single(e => e.TargetFramework == NuGetFramework.Parse("net45")).Packages.FirstOrDefault(e => e.Id == "c");
                var dependencyCNet46 = groups.Single(e => e.TargetFramework == NuGetFramework.Parse("net46")).Packages.FirstOrDefault(e => e.Id == "c");

                // Assert
                dependencyCNet45.VersionRange.Should().Be(VersionRange.Parse("[3.0.0]"));
                dependencyCNet46.VersionRange.Should().Be(VersionRange.Parse("[3.0.0]"));
                nuspecA.GetDependencyGroups().Count().Should().Be(2);
            }
        }

        private static NuspecReader GetNuspec(string path)
        {
            using (var reader = new PackageArchiveReader(path))
            {
                return reader.NuspecReader;
            }
        }
    }
}