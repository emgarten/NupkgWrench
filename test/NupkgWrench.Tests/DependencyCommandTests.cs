using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using AwesomeAssertions;
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
                groups["net45"].Packages.Should().BeEquivalentTo(depGroup1.Packages);
                groups["net46"].Packages.Should().BeEquivalentTo(depGroup2.Packages);
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
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").Include.Should().BeEquivalentTo(new[] { "build" });
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").Exclude.Should().BeEquivalentTo(new[] { "content" });
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
                groups["net45"].Packages.Single(e => e.Id == "b").Exclude.Should().BeEquivalentTo(new[] { "compile", "runtime" });
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").VersionRange.Should().Be(VersionRange.Parse("2.0.0"));
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").Include.Should().BeEquivalentTo(new[] { "build" });
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").Exclude.Should().BeEquivalentTo(new[] { "compile", "runtime" });
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
                groups["net45"].Packages.Single(e => e.Id == "b").Include.Should().BeEquivalentTo(new[] { "compile", "runtime" });
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").VersionRange.Should().Be(VersionRange.Parse("2.0.0"));
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").Include.Should().BeEquivalentTo(new[] { "compile", "runtime" });
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").Exclude.Should().BeEquivalentTo(new[] { "content" });
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
                groups["net45"].Packages.Single(e => e.Id == "b").Include.Should().BeEquivalentTo(new[] { "compile" });
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").VersionRange.Should().Be(VersionRange.Parse("2.0.0"));
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").Include.Should().BeEquivalentTo(new[] { "compile" });
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").Exclude.Should().BeEmpty();
                groups["any"].Packages.FirstOrDefault(e => e.Id == "x").VersionRange.Should().Be(VersionRange.Parse("1.0.0"));
                groups["any"].Packages.FirstOrDefault(e => e.Id == "x").Include.Should().BeEquivalentTo(new[] { "compile" });
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
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").Exclude.Should().BeEquivalentTo(new[] { "content" });
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
                groups["any"].Packages.FirstOrDefault(e => e.Id == "b").Include.Should().BeEquivalentTo(new[] { "build" });
                groups["any"].Packages.FirstOrDefault(e => e.Id == "x").VersionRange.Should().Be(VersionRange.Parse("1.0.0"));
                groups["any"].Packages.FirstOrDefault(e => e.Id == "x").Include.Should().BeEquivalentTo(new[] { "build" });
                groups["any"].Packages.FirstOrDefault(e => e.Id == "x").Exclude.Should().BeEquivalentTo(new[] { "content" });
            }
        }

        [Theory]
        [InlineData("a")]
        [InlineData("b")]
        [InlineData("c")]
        [InlineData("f")]
        [InlineData("z")]
        public async Task DependencyCommandTests_RemoveWithNoDependencyGroup(string packageId)
        {
            using (var workingDir = new TestFolder())
            {
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        XMLOverride = XDocument.Parse(Properties.Resources.NuspecWithNoDependencyGroupString)
                    }
                };

                var zipFile = testPackage.Save(workingDir.Root);
                var log = new TestLogger();

                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "remove", workingDir.Root, "--dependency-id", packageId }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var pr = new PackageArchiveReader(zipFile.FullName);
                var dependencies = pr.GetPackageDependencies();
                dependencies.Count().Should().Be(1);
                var dependency = dependencies.First();
                dependency.Packages.Should().NotContain(p => string.Equals(packageId, p.Id, System.StringComparison.OrdinalIgnoreCase));
            }
        }

        [Fact]
        public async Task DependencyCommandTests_ModifyWithNoDependencyGroup()
        {
            using (var workingDir = new TestFolder())
            {
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        XMLOverride = XDocument.Parse(Properties.Resources.NuspecWithNoDependencyGroupString)
                    }
                };

                var zipFile = testPackage.Save(workingDir.Root);
                var log = new TestLogger();

                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "modify", workingDir.Root, "--dependency-id", "a", "--dependency-version", "[2.0.0, 3.0.0)" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var pr = new PackageArchiveReader(zipFile.FullName);
                var dependencies = pr.GetPackageDependencies();
                dependencies.Count().Should().Be(1);
                var deps = dependencies.First().Packages.ToDictionary(e => e.Id);

                deps["a"].VersionRange.ToNormalizedString().Should().Be("[2.0.0, 3.0.0)");
                deps["b"].VersionRange.ToNormalizedString().Should().Be("[1.0.0, )");
            }
        }

        [Fact]
        public async Task DependencyCommandTests_AddWithNoDependencyGroup()
        {
            using (var workingDir = new TestFolder())
            {
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        XMLOverride = XDocument.Parse(Properties.Resources.NuspecWithNoDependencyGroupString)
                    }
                };

                var zipFile = testPackage.Save(workingDir.Root);
                var log = new TestLogger();

                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "add", workingDir.Root, "--dependency-id", "z", "--dependency-version", "2.0.0" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var pr = new PackageArchiveReader(zipFile.FullName);
                var dependencies = pr.GetPackageDependencies();
                dependencies.Count().Should().Be(1);
                var deps = dependencies.First().Packages.ToDictionary(e => e.Id);
                deps.Count.Should().Be(7);

                deps["z"].VersionRange.ToNormalizedString().Should().Be("[2.0.0, )");
                deps["b"].VersionRange.ToNormalizedString().Should().Be("[1.0.0, )");
            }
        }

        [Fact]
        public async Task DependencyCommandTests_AddEmptyGroupWithNoDependencyGroup()
        {
            using (var workingDir = new TestFolder())
            {
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        XMLOverride = XDocument.Parse(Properties.Resources.NuspecWithNoDependencyGroupString)
                    }
                };

                var zipFile = testPackage.Save(workingDir.Root);
                var log = new TestLogger();

                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "emptygroup", workingDir.Root, "--framework", "netstandard1.6" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var pr = new PackageArchiveReader(zipFile.FullName);
                var dependencies = pr.GetPackageDependencies();
                dependencies.Count().Should().Be(2);
            }
        }

        [Fact]
        public async Task DependencyCommandTests_ClearWithNoDependencyGroup()
        {
            using (var workingDir = new TestFolder())
            {
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        XMLOverride = XDocument.Parse(Properties.Resources.NuspecWithNoDependencyGroupString)
                    }
                };

                var zipFile = testPackage.Save(workingDir.Root);
                var log = new TestLogger();

                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "clear", workingDir.Root }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var pr = new PackageArchiveReader(zipFile.FullName);
                var dependencies = pr.GetPackageDependencies();
                dependencies.Count().Should().Be(0);
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
                groups["net45"].Packages.Should().BeEquivalentTo(depGroup1.Packages);
                groups["net46"].Packages.Should().BeEquivalentTo(depGroup2.Packages);
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
                groups["net45"].Packages.Should().BeEquivalentTo(depGroup1.Packages);
                groups["net46"].Packages.Should().BeEquivalentTo(depGroup2.Packages);
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
                groups["net45"].Packages.Should().BeEquivalentTo(depGroup1.Packages);
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
                groups["net45"].Packages.Should().BeEquivalentTo(depGroup1.Packages);
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
        public async Task DependencyCommandTests_AddToMultiplePackagesVerifyEachPackageKeepsItsGroups()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                // a has no groups and is processed before b.
                var testPackageA = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var testPackageB = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "1.0.0"
                    }
                };

                testPackageB.Nuspec.Dependencies.Add(new PackageDependencyGroup(NuGetFramework.Parse("net45"), new[] {
                    new PackageDependency("x", VersionRange.Parse("1.0.0"))
                }));

                testPackageB.Nuspec.Dependencies.Add(new PackageDependencyGroup(NuGetFramework.Parse("net46"), new[] {
                    new PackageDependency("x", VersionRange.Parse("1.0.0"))
                }));

                var zipFileA = testPackageA.Save(workingDir.Root);
                var zipFileB = testPackageB.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "add", workingDir.Root, "--dependency-id", "y", "--dependency-version", "1.0.0" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var groupsA = GetNuspec(zipFileA.FullName).GetDependencyGroups().ToDictionary(e => e.TargetFramework.GetShortFolderName().ToLowerInvariant());
                var groupsB = GetNuspec(zipFileB.FullName).GetDependencyGroups().ToDictionary(e => e.TargetFramework.GetShortFolderName().ToLowerInvariant());

                // Assert
                groupsA.Count.Should().Be(1);
                groupsA["any"].Packages.Select(e => e.Id).Should().BeEquivalentTo(new[] { "y" });
                groupsB.Count.Should().Be(2);
                groupsB["net45"].Packages.Select(e => e.Id).Should().BeEquivalentTo(new[] { "x", "y" });
                groupsB["net46"].Packages.Select(e => e.Id).Should().BeEquivalentTo(new[] { "x", "y" });
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

        [Theory]
        [InlineData("net6.0-windows")]
        [InlineData("net8.0-android34.0")]
        [InlineData("net40-client")]
        public async Task DependencyCommandTests_AddTwiceWithPlatformOrProfileFrameworkVerifySingleGroup(string framework)
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

                var zipFileA = testPackageA.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "add", workingDir.Root, "--dependency-id", "b", "--dependency-version", "1.0.0", "--framework", framework }, log);
                exitCode.Should().Be(0, log.GetMessages());

                exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "add", workingDir.Root, "--dependency-id", "c", "--dependency-version", "1.0.0", "--framework", framework }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspecA = GetNuspec(zipFileA.FullName);
                var group = nuspecA.GetDependencyGroups().Single();

                // Assert
                group.TargetFramework.Should().Be(NuGetFramework.Parse(framework));
                group.Packages.Select(e => e.Id).Should().BeEquivalentTo(new[] { "b", "c" });
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

        [Fact]
        public async Task DependencyCommandTests_AddWithNoDependenciesElement()
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

                var zipFileA = testPackageA.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "add", workingDir.Root, "--dependency-id", "b", "--dependency-version", "1.0.0" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspecA = GetNuspec(zipFileA.FullName);
                var group = nuspecA.GetDependencyGroups().Single();

                // Assert
                group.TargetFramework.IsAny.Should().BeTrue();
                group.Packages.Single().Id.Should().Be("b");
                group.Packages.Single().VersionRange.Should().Be(VersionRange.Parse("1.0.0"));
            }
        }

        [Fact]
        public async Task DependencyCommandTests_AddWithExcludeAndInclude()
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

                var zipFileA = testPackageA.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "add", workingDir.Root, "--dependency-id", "b", "--dependency-version", "1.0.0", "--dependency-exclude", "build", "--dependency-include", "compile" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspecA = GetNuspec(zipFileA.FullName);
                var dependency = nuspecA.GetDependencyGroups().Single().Packages.Single();

                // Assert
                dependency.Id.Should().Be("b");
                dependency.Exclude.Should().BeEquivalentTo(new[] { "build" });
                dependency.Include.Should().BeEquivalentTo(new[] { "compile" });
            }
        }

        [Fact]
        public async Task DependencyCommandTests_ModifyAllInFramework()
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
                    new PackageDependency("b", VersionRange.Parse("1.0.0")),
                    new PackageDependency("c", VersionRange.Parse("1.0.0"))
                });

                var depGroup2 = new PackageDependencyGroup(NuGetFramework.Parse("net46"), new[] {
                    new PackageDependency("b", VersionRange.Parse("1.0.0"))
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup1);
                testPackageA.Nuspec.Dependencies.Add(depGroup2);

                var zipFileA = testPackageA.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "modify", workingDir.Root, "--framework", "net45", "--dependency-version", "2.0.0" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspecA = GetNuspec(zipFileA.FullName);
                var groups = nuspecA.GetDependencyGroups().ToDictionary(e => e.TargetFramework.GetShortFolderName().ToLowerInvariant());

                // Assert
                groups.Count.Should().Be(2);
                groups["net45"].Packages.Select(e => e.Id).Should().BeEquivalentTo(new[] { "b", "c" });
                groups["net45"].Packages.Should().OnlyContain(e => e.VersionRange.Equals(VersionRange.Parse("2.0.0")));
                groups["net46"].Packages.Should().BeEquivalentTo(depGroup2.Packages);
            }
        }

        [Fact]
        public async Task DependencyCommandTests_ModifyWithNoEditOptionsFails()
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

                testPackageA.Nuspec.Dependencies.Add(depGroup1);

                var zipFileA = testPackageA.Save(workingDir.Root);
                var before = File.ReadAllBytes(zipFileA.FullName);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "modify", workingDir.Root, "--dependency-id", "b" }, log);

                // Assert
                exitCode.Should().Be(1);
                log.GetMessages().Should().Contain("One of the following options must be specified");
                File.ReadAllBytes(zipFileA.FullName).Should().Equal(before);
            }
        }

        [Fact]
        public async Task DependencyCommandTests_RemoveWithNoIdAndFrameworkVerifyOnlyThatGroupIsEmptied()
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
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "remove", workingDir.Root, "--framework", "net45" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspecA = GetNuspec(zipFileA.FullName);
                var groups = nuspecA.GetDependencyGroups().ToDictionary(e => e.TargetFramework.GetShortFolderName().ToLowerInvariant());

                // Assert
                groups.Count.Should().Be(2);
                groups["net45"].Packages.Should().BeEmpty();
                groups["net46"].Packages.Should().BeEquivalentTo(depGroup2.Packages);
            }
        }

        [Fact]
        public async Task DependencyCommandTests_ClearAnyWithNoDependencyGroup()
        {
            using (var workingDir = new TestFolder())
            {
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        XMLOverride = XDocument.Parse(Properties.Resources.NuspecWithNoDependencyGroupString)
                    }
                };

                var zipFile = testPackage.Save(workingDir.Root);
                var log = new TestLogger();

                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "clear", workingDir.Root, "--framework", "any" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspec = GetNuspec(zipFile.FullName);
                nuspec.GetDependencyGroups().Should().BeEmpty();
            }
        }

        [Fact]
        public async Task DependencyCommandTests_ClearAnyVerifyFrameworkGroupsAreKept()
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

                var depGroup1 = new PackageDependencyGroup(NuGetFramework.AnyFramework, new[] {
                    new PackageDependency("b", VersionRange.Parse("1.0.0"))
                });

                var depGroup2 = new PackageDependencyGroup(NuGetFramework.Parse("net45"), new[] {
                    new PackageDependency("c", VersionRange.Parse("1.0.0"))
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup1);
                testPackageA.Nuspec.Dependencies.Add(depGroup2);

                var zipFileA = testPackageA.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "clear", workingDir.Root, "--framework", "any" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspecA = GetNuspec(zipFileA.FullName);
                var group = nuspecA.GetDependencyGroups().Single();

                // Assert
                group.TargetFramework.Should().Be(NuGetFramework.Parse("net45"));
                group.Packages.Should().BeEquivalentTo(depGroup2.Packages);
            }
        }

        [Fact]
        public async Task DependencyCommandTests_EmptyGroupWithNoDependenciesElement()
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

                var zipFileA = testPackageA.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "emptygroup", workingDir.Root, "--framework", "net45" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspecA = GetNuspec(zipFileA.FullName);
                var group = nuspecA.GetDependencyGroups().Single();

                // Assert
                group.TargetFramework.Should().Be(NuGetFramework.Parse("net45"));
                group.Packages.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task DependencyCommandTests_EmptyGroupWithNoDependencyGroupVerifyDependenciesMoveToAnyGroup()
        {
            using (var workingDir = new TestFolder())
            {
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        XMLOverride = XDocument.Parse(Properties.Resources.NuspecWithNoDependencyGroupString)
                    }
                };

                var zipFile = testPackage.Save(workingDir.Root);
                var log = new TestLogger();

                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "emptygroup", workingDir.Root, "--framework", "net45" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspec = GetNuspec(zipFile.FullName);
                var groups = nuspec.GetDependencyGroups().ToDictionary(e => e.TargetFramework.GetShortFolderName().ToLowerInvariant());

                groups.Count.Should().Be(2);
                groups["any"].Packages.Select(e => e.Id).Should().BeEquivalentTo(new[] { "a", "b", "c", "d", "e", "f" });
                groups["net45"].Packages.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task DependencyCommandTests_EmptyGroupWithSingleUngroupedDependencyVerifyDependencyMovesToAnyGroup()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        XMLOverride = CreateNuspec(@"<dependency id=""b"" version=""1.0.0"" />")
                    }
                };

                var zipFile = testPackage.Save(workingDir.Root);
                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "emptygroup", workingDir.Root, "--framework", "net45" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspec = GetNuspec(zipFile.FullName);
                var groups = nuspec.GetDependencyGroups().ToDictionary(e => e.TargetFramework.GetShortFolderName().ToLowerInvariant());

                // Assert
                groups.Count.Should().Be(2);
                groups["any"].Packages.Select(e => e.Id).Should().BeEquivalentTo(new[] { "b" });
                groups["net45"].Packages.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task DependencyCommandTests_EmptyGroupWithUngroupedDependencyAndGroupsVerifyUngroupedDependencyStaysIgnored()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                // NuGet ignores ungrouped dependencies when groups exist, so b and d are not dependencies before or after.
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        XMLOverride = CreateNuspec(@"<dependency id=""b"" version=""1.0.0"" />
<dependency id=""d"" version=""1.0.0"" />
<group targetFramework=""net46""><dependency id=""c"" version=""1.0.0"" /></group>")
                    }
                };

                var zipFile = testPackage.Save(workingDir.Root);
                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "emptygroup", workingDir.Root, "--framework", "net45" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspec = GetNuspec(zipFile.FullName);
                var groups = nuspec.GetDependencyGroups().ToDictionary(e => e.TargetFramework.GetShortFolderName().ToLowerInvariant());
                var rootDependencies = Util.GetMetadataElement(nuspec.Xml).Elements().Single(e => e.Name.LocalName == "dependencies")
                    .Elements().Where(e => e.Name.LocalName == "dependency")
                    .Select(e => e.Attribute("id").Value);

                // Assert
                groups.Count.Should().Be(2);
                groups["net46"].Packages.Select(e => e.Id).Should().BeEquivalentTo(new[] { "c" });
                groups["net45"].Packages.Should().BeEmpty();
                rootDependencies.Should().BeEquivalentTo(new[] { "b", "d" });
            }
        }

        [Theory]
        [InlineData("net6.0-windows")]
        [InlineData("net8.0-android34.0")]
        [InlineData("portable-net45+win8")]
        public async Task DependencyCommandTests_EmptyGroupTwiceWithPlatformOrProfileFrameworkVerifySingleGroup(string framework)
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

                var zipFileA = testPackageA.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "emptygroup", workingDir.Root, "--framework", framework }, log);
                exitCode.Should().Be(0, log.GetMessages());

                exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "emptygroup", workingDir.Root, "--framework", framework }, log);
                exitCode.Should().Be(0, log.GetMessages());

                var nuspecA = GetNuspec(zipFileA.FullName);
                var group = nuspecA.GetDependencyGroups().Single();

                // Assert
                group.TargetFramework.Should().Be(NuGetFramework.Parse(framework));
                group.Packages.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task DependencyCommandTests_EmptyGroupWithMultiplePackagesVerifyEachPackageIsUpdated()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                // a and b have a net45 group and are processed before c, which has no groups.
                var testPackageA = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                testPackageA.Nuspec.Dependencies.Add(new PackageDependencyGroup(NuGetFramework.Parse("net45"), new[] {
                    new PackageDependency("x", VersionRange.Parse("1.0.0"))
                }));

                var testPackageB = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "1.0.0"
                    }
                };

                testPackageB.Nuspec.Dependencies.Add(new PackageDependencyGroup(NuGetFramework.Parse("net45"), new[] {
                    new PackageDependency("x", VersionRange.Parse("1.0.0"))
                }));

                var testPackageC = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "c",
                        Version = "1.0.0"
                    }
                };

                var zipFiles = new[]
                {
                    testPackageA.Save(workingDir.Root),
                    testPackageB.Save(workingDir.Root),
                    testPackageC.Save(workingDir.Root)
                };

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "emptygroup", workingDir.Root, "--framework", "net45" }, log);
                exitCode.Should().Be(0, log.GetMessages());

                // Assert
                foreach (var zipFile in zipFiles)
                {
                    var groups = GetNuspec(zipFile.FullName).GetDependencyGroups().ToList();

                    groups.Should().ContainSingle(zipFile.Name);
                    groups[0].TargetFramework.Should().Be(NuGetFramework.Parse("net45"), zipFile.Name);
                    groups[0].Packages.Should().BeEmpty(zipFile.Name);
                }
            }
        }

        [Fact]
        public async Task DependencyCommandTests_EmptyGroupWithNoFrameworkFails()
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

                var zipFileA = testPackageA.Save(workingDir.Root);
                var before = File.ReadAllBytes(zipFileA.FullName);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "emptygroup", workingDir.Root }, log);

                // Assert
                exitCode.Should().Be(1);
                log.GetMessages().Should().Contain("Missing required parameter --framework.");
                File.ReadAllBytes(zipFileA.FullName).Should().Equal(before);
            }
        }

        private static NuspecReader GetNuspec(string path)
        {
            using (var reader = new PackageArchiveReader(path))
            {
                return reader.NuspecReader;
            }
        }

        private static XDocument CreateNuspec(string dependencies)
        {
            return XDocument.Parse($@"<package xmlns=""http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"">
  <metadata>
    <id>a</id>
    <version>1.0.0</version>
    <authors>author</authors>
    <description>My package description.</description>
    <dependencies>
{dependencies}
    </dependencies>
  </metadata>
</package>");
        }
    }
}