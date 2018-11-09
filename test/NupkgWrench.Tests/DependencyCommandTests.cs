using System.IO;
using System.Linq;
using System.Text;
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
        public async Task DependencyCommandTests_ModifyMissingPackage()
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
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "modify", workingDir.Root, "--dependency-id", "x", "--dependency-version", "5.0.0" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.1.0.0.nupkg"));
                var groups = nuspecA.GetDependencyGroups().ToDictionary(e => e.TargetFramework.GetShortFolderName().ToLowerInvariant());

                // Assert
                groups.Count().Should().Be(2);
                groups["net45"].Packages.ShouldBeEquivalentTo(depGroup1.Packages);
                groups["net46"].Packages.ShouldBeEquivalentTo(depGroup2.Packages);
            }
        }

        [Fact]
        public async Task DependencyCommandTests_ModifyVersion()
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

                var depGroup2 = new PackageDependencyGroup(NuGetFramework.Parse("any"), new[] {
                    new PackageDependency("b", VersionRange.Parse("2.0.0"), new[] { "build" }, new[] { "content" }),
                    new PackageDependency("x", VersionRange.Parse("1.0.0"))
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup1);
                testPackageA.Nuspec.Dependencies.Add(depGroup2);

                var zipFileA = testPackageA.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "modify", workingDir.Root, "--dependency-id", "b", "--dependency-version", "5.0.0" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.1.0.0.nupkg"));
                var groups = nuspecA.GetDependencyGroups().ToDictionary(e => e.TargetFramework.GetShortFolderName().ToLowerInvariant());

                // Assert
                groups.Count().Should().Be(2);
                groups["net45"].Packages.Single(e => e.Id == "b").VersionRange.Should().Be(VersionRange.Parse("5.0.0"));
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").VersionRange.Should().Be(VersionRange.Parse("5.0.0"));
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").Include.ShouldBeEquivalentTo(new[] { "build" });
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").Exclude.ShouldBeEquivalentTo(new[] { "content" });
                groups["any"].Packages.FirstOrDefault(e => e.Id == "x").VersionRange.Should().Be(VersionRange.Parse("1.0.0"));
            }
        }

        [Fact]
        public async Task DependencyCommandTests_SetExclude()
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

                var depGroup2 = new PackageDependencyGroup(NuGetFramework.Parse("any"), new[] {
                    new PackageDependency("b", VersionRange.Parse("2.0.0"), new[] { "build" }, new[] { "content" }),
                    new PackageDependency("x", VersionRange.Parse("1.0.0"))
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup1);
                testPackageA.Nuspec.Dependencies.Add(depGroup2);

                var zipFileA = testPackageA.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "modify", workingDir.Root, "--dependency-id", "b", "--dependency-exclude", "compile,runtime" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.1.0.0.nupkg"));
                var groups = nuspecA.GetDependencyGroups().ToDictionary(e => e.TargetFramework.GetShortFolderName().ToLowerInvariant());

                // Assert
                groups.Count().Should().Be(2);
                groups["net45"].Packages.Single(e => e.Id == "b").VersionRange.Should().Be(VersionRange.Parse("1.0.0"));
                groups["net45"].Packages.Single(e => e.Id == "b").Include.Should().BeEmpty();
                groups["net45"].Packages.Single(e => e.Id == "b").Exclude.ShouldBeEquivalentTo(new[] { "compile", "runtime" });
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").VersionRange.Should().Be(VersionRange.Parse("2.0.0"));
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").Include.ShouldBeEquivalentTo(new[] { "build" });
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").Exclude.ShouldBeEquivalentTo(new[] { "compile", "runtime" });
                groups["any"].Packages.FirstOrDefault(e => e.Id == "x").VersionRange.Should().Be(VersionRange.Parse("1.0.0"));
                groups["any"].Packages.FirstOrDefault(e => e.Id == "x").Include.Should().BeEmpty();
                groups["any"].Packages.FirstOrDefault(e => e.Id == "x").Exclude.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task DependencyCommandTests_SetInclude()
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

                var depGroup2 = new PackageDependencyGroup(NuGetFramework.Parse("any"), new[] {
                    new PackageDependency("b", VersionRange.Parse("2.0.0"), new[] { "build" }, new[] { "content" }),
                    new PackageDependency("x", VersionRange.Parse("1.0.0"))
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup1);
                testPackageA.Nuspec.Dependencies.Add(depGroup2);

                var zipFileA = testPackageA.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "modify", workingDir.Root, "--dependency-id", "b", "--dependency-include", "compile,runtime" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.1.0.0.nupkg"));
                var groups = nuspecA.GetDependencyGroups().ToDictionary(e => e.TargetFramework.GetShortFolderName().ToLowerInvariant());

                // Assert
                groups.Count().Should().Be(2);
                groups["net45"].Packages.Single(e => e.Id == "b").VersionRange.Should().Be(VersionRange.Parse("1.0.0"));
                groups["net45"].Packages.Single(e => e.Id == "b").Exclude.Should().BeEmpty();
                groups["net45"].Packages.Single(e => e.Id == "b").Include.ShouldBeEquivalentTo(new[] { "compile", "runtime" });
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").VersionRange.Should().Be(VersionRange.Parse("2.0.0"));
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").Include.ShouldBeEquivalentTo(new[] { "compile", "runtime" });
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").Exclude.ShouldBeEquivalentTo(new[] { "content" });
                groups["any"].Packages.FirstOrDefault(e => e.Id == "x").VersionRange.Should().Be(VersionRange.Parse("1.0.0"));
                groups["any"].Packages.FirstOrDefault(e => e.Id == "x").Include.Should().BeEmpty();
                groups["any"].Packages.FirstOrDefault(e => e.Id == "x").Exclude.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task DependencyCommandTests_SetOnAll()
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

                var depGroup2 = new PackageDependencyGroup(NuGetFramework.Parse("any"), new[] {
                    new PackageDependency("b", VersionRange.Parse("2.0.0"), new[] { "build" }, new[] { "content" }),
                    new PackageDependency("x", VersionRange.Parse("1.0.0"))
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup1);
                testPackageA.Nuspec.Dependencies.Add(depGroup2);

                var zipFileA = testPackageA.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "modify", workingDir.Root, "--dependency-include", "compile", "--clear-exclude" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.1.0.0.nupkg"));
                var groups = nuspecA.GetDependencyGroups().ToDictionary(e => e.TargetFramework.GetShortFolderName().ToLowerInvariant());

                // Assert
                groups.Count().Should().Be(2);
                groups["net45"].Packages.Single(e => e.Id == "b").VersionRange.Should().Be(VersionRange.Parse("1.0.0"));
                groups["net45"].Packages.Single(e => e.Id == "b").Exclude.Should().BeEmpty();
                groups["net45"].Packages.Single(e => e.Id == "b").Include.ShouldBeEquivalentTo(new[] { "compile" });
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").VersionRange.Should().Be(VersionRange.Parse("2.0.0"));
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").Include.ShouldBeEquivalentTo(new[] { "compile" });
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").Exclude.Should().BeEmpty();
                groups["any"].Packages.FirstOrDefault(e => e.Id == "x").VersionRange.Should().Be(VersionRange.Parse("1.0.0"));
                groups["any"].Packages.FirstOrDefault(e => e.Id == "x").Include.ShouldBeEquivalentTo(new[] { "compile" });
                groups["any"].Packages.FirstOrDefault(e => e.Id == "x").Exclude.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task DependencyCommandTests_ClearInclude()
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

                var depGroup2 = new PackageDependencyGroup(NuGetFramework.Parse("any"), new[] {
                    new PackageDependency("b", VersionRange.Parse("2.0.0"), new[] { "build" }, new[] { "content" }),
                    new PackageDependency("x", VersionRange.Parse("1.0.0"))
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup1);
                testPackageA.Nuspec.Dependencies.Add(depGroup2);

                var zipFileA = testPackageA.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "modify", workingDir.Root, "--dependency-id", "b", "--clear-include" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.1.0.0.nupkg"));
                var groups = nuspecA.GetDependencyGroups().ToDictionary(e => e.TargetFramework.GetShortFolderName().ToLowerInvariant());

                // Assert
                groups.Count().Should().Be(2);
                groups["net45"].Packages.Single(e => e.Id == "b").VersionRange.Should().Be(VersionRange.Parse("1.0.0"));
                groups["net45"].Packages.Single(e => e.Id == "b").Exclude.Should().BeEmpty();
                groups["net45"].Packages.Single(e => e.Id == "b").Include.Should().BeEmpty();
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").VersionRange.Should().Be(VersionRange.Parse("2.0.0"));
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").Include.Should().BeEmpty();
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").Exclude.ShouldBeEquivalentTo(new[] { "content" });
                groups["any"].Packages.FirstOrDefault(e => e.Id == "x").VersionRange.Should().Be(VersionRange.Parse("1.0.0"));
                groups["any"].Packages.FirstOrDefault(e => e.Id == "x").Include.Should().BeEmpty();
                groups["any"].Packages.FirstOrDefault(e => e.Id == "x").Exclude.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task DependencyCommandTests_ClearExclude()
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

                var depGroup2 = new PackageDependencyGroup(NuGetFramework.Parse("any"), new[] {
                    new PackageDependency("b", VersionRange.Parse("2.0.0"), new[] { "build" }, new[] { "content" }),
                    new PackageDependency("x", VersionRange.Parse("1.0.0"), new[] { "build" }, new[] { "content" }),
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup1);
                testPackageA.Nuspec.Dependencies.Add(depGroup2);

                var zipFileA = testPackageA.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "modify", workingDir.Root, "--dependency-id", "b", "--clear-exclude" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.1.0.0.nupkg"));
                var groups = nuspecA.GetDependencyGroups().ToDictionary(e => e.TargetFramework.GetShortFolderName().ToLowerInvariant());

                // Assert
                groups.Count().Should().Be(2);
                groups["net45"].Packages.Single(e => e.Id == "b").VersionRange.Should().Be(VersionRange.Parse("1.0.0"));
                groups["net45"].Packages.Single(e => e.Id == "b").Exclude.Should().BeEmpty();
                groups["net45"].Packages.Single(e => e.Id == "b").Include.Should().BeEmpty();
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").VersionRange.Should().Be(VersionRange.Parse("2.0.0"));
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").Exclude.Should().BeEmpty();
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").Include.ShouldBeEquivalentTo(new[] { "build" });
                groups["any"].Packages.FirstOrDefault(e => e.Id == "x").VersionRange.Should().Be(VersionRange.Parse("1.0.0"));
                groups["any"].Packages.FirstOrDefault(e => e.Id == "x").Include.ShouldBeEquivalentTo(new[] { "build" });
                groups["any"].Packages.FirstOrDefault(e => e.Id == "x").Exclude.ShouldBeEquivalentTo(new[] { "content" });
            }
        }

        [Theory]
        [InlineData("a")]
        [InlineData("b")]
        [InlineData("c")]
        [InlineData("f")]
        [InlineData("z")]
        public async Task DependencyCommandTests_RemoveWithNoDepdencyGroup(string packageId)
        {
            using (var workingDir = new TestFolder())
            {
                var specFile = Path.Combine(workingDir.Root, "test.nuspec");
                File.WriteAllText(specFile, Properties.Resources.NuspecWithNoDependencyGroup, Encoding.UTF8);
                var pb = new PackageBuilder(specFile, workingDir.Root, null, false);

                var pkgFile = Path.Combine(workingDir.Root, "pkg.nupkg");
                using (var writePackage = new FileStream(pkgFile, FileMode.OpenOrCreate))
                {
                    pb.Save(writePackage);
                }

                var log = new TestLogger();

                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "remove", workingDir.Root, "--dependency-id", packageId }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var pr = new PackageArchiveReader(pkgFile);
                var dependencies = pr.GetPackageDependencies();
                dependencies.Count().Should().Be(1);
                var dependency = dependencies.First();
                dependency.Packages.Should().NotContain(p => string.Equals(packageId, p.Id, System.StringComparison.OrdinalIgnoreCase));
            }
        }

        [Fact]
        public async Task DependencyCommandTests_RemoveWithNoIdVerifyEmptyGroups()
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
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "remove", workingDir.Root }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.1.0.0.nupkg"));
                var groups = nuspecA.GetDependencyGroups().ToDictionary(e => e.TargetFramework.GetShortFolderName().ToLowerInvariant());

                // Assert
                groups.Count().Should().Be(2);
                groups["net45"].Packages.Should().BeEmpty();
                groups["net46"].Packages.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task DependencyCommandTests_RemoveMissingPackage()
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
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "remove", workingDir.Root, "--dependency-id", "x" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.1.0.0.nupkg"));
                var groups = nuspecA.GetDependencyGroups().ToDictionary(e => e.TargetFramework.GetShortFolderName().ToLowerInvariant());

                // Assert
                groups.Count().Should().Be(2);
                groups["net45"].Packages.ShouldBeEquivalentTo(depGroup1.Packages);
                groups["net46"].Packages.ShouldBeEquivalentTo(depGroup2.Packages);
            }
        }

        [Fact]
        public async Task DependencyCommandTests_RemoveWithFrameworkThatDoesNotExist()
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
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "remove", workingDir.Root, "--dependency-id", "b", "--framework", "net47" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.1.0.0.nupkg"));
                var groups = nuspecA.GetDependencyGroups().ToDictionary(e => e.TargetFramework.GetShortFolderName().ToLowerInvariant());

                // Assert
                groups.Count().Should().Be(2);
                groups["net45"].Packages.ShouldBeEquivalentTo(depGroup1.Packages);
                groups["net46"].Packages.ShouldBeEquivalentTo(depGroup2.Packages);
            }
        }

        [Fact]
        public async Task DependencyCommandTests_RemoveWithFrameworkThatDoesExist()
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
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "remove", workingDir.Root, "--dependency-id", "b", "--framework", "net46" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.1.0.0.nupkg"));
                var groups = nuspecA.GetDependencyGroups().ToDictionary(e => e.TargetFramework.GetShortFolderName().ToLowerInvariant());

                // Assert
                groups.Count().Should().Be(2);
                groups["net45"].Packages.ShouldBeEquivalentTo(depGroup1.Packages);
                groups["net46"].Packages.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task DependencyCommandTests_RemoveFromSingleGroup()
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
                    new PackageDependency("x", VersionRange.Parse("2.0.0"))
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup1);
                testPackageA.Nuspec.Dependencies.Add(depGroup2);

                var zipFileA = testPackageA.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "remove", workingDir.Root, "--dependency-id", "x" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.1.0.0.nupkg"));
                var groups = nuspecA.GetDependencyGroups().ToDictionary(e => e.TargetFramework.GetShortFolderName().ToLowerInvariant());

                // Assert
                groups.Count().Should().Be(2);
                groups["net45"].Packages.ShouldBeEquivalentTo(depGroup1.Packages);
                groups["net46"].Packages.Should().BeEmpty();
            }
        }

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