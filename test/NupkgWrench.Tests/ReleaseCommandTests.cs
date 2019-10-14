using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Test.Helpers;
using NuGet.Versioning;
using Xunit;

namespace NupkgWrench.Tests
{
    public class ReleaseCommandTests
    {
        [Fact]
        public async Task Command_ReleaseCommand_MultipleNewVersions_EachTFMTakesCorrectVersion()
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

                var testPackageB1 = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "1.0.0"
                    }
                };

                var testPackageB2 = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "2.0.0"
                    }
                };

                var testPackageB3 = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "0.1.0"
                    }
                };

                var testPackageB4 = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "9.0.0"
                    }
                };

                var zipFileA = testPackageA.Save(workingDir.Root);
                var zipFileB1 = testPackageB1.Save(workingDir.Root);
                var zipFileB2 = testPackageB2.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "release", workingDir.Root, "-r", "beta" }, log);

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.1.0.0-beta.nupkg"));
                var dependencyBNet45 = nuspecA.GetDependencyGroups().Single(e => e.TargetFramework == NuGetFramework.Parse("net45")).Packages.Single(e => e.Id == "b");
                var dependencyBNet46 = nuspecA.GetDependencyGroups().Single(e => e.TargetFramework == NuGetFramework.Parse("net46")).Packages.Single(e => e.Id == "b");

                // Assert
                Assert.Equal(0, exitCode);

                Assert.Equal("1.0.0-beta", dependencyBNet45.VersionRange.ToLegacyShortString());
                Assert.Equal("2.0.0-beta", dependencyBNet46.VersionRange.ToLegacyShortString());
            }
        }

        [Fact]
        public async Task Command_ReleaseCommand_MultipleNewVersions_TakeLowestValid()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackageA = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "6.0.0"
                    }
                };

                var depGroup = new PackageDependencyGroup(NuGetFramework.Parse("net45"), new[] {
                    new PackageDependency("b", VersionRange.Parse("5.0.0"))
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup);

                var testPackageB1 = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "6.0.0"
                    }
                };

                var testPackageB2 = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "7.0.0"
                    }
                };

                var testPackageB3 = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "1.0.0"
                    }
                };

                var zipFileA = testPackageA.Save(workingDir.Root);
                var zipFileB1 = testPackageB1.Save(workingDir.Root);
                var zipFileB2 = testPackageB2.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "release", workingDir.Root, "-r", "beta" }, log);

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.6.0.0-beta.nupkg"));
                var dependencyB = nuspecA.GetDependencyGroups().Single().Packages.Single(e => e.Id == "b");
                var dependencyBString = dependencyB.VersionRange.ToLegacyShortString();

                // Assert
                Assert.Equal(0, exitCode);

                Assert.Equal("6.0.0-beta", dependencyBString);
            }
        }

        [Fact]
        public async Task Command_ReleaseCommand_NewMinVersionNonInclusive_SwitchesMode()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackageA = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "6.0.0"
                    }
                };

                var depGroup = new PackageDependencyGroup(NuGetFramework.Parse("net45"), new[] {
                    new PackageDependency("b", VersionRange.Parse("(6.0.0, )"))
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup);

                var testPackageB = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "7.0.0"
                    }
                };

                var zipFileA = testPackageA.Save(workingDir.Root);
                var zipFileB = testPackageB.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "release", workingDir.Root, "-n", "6.0.0" }, log);

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.6.0.0.nupkg"));
                var dependencyB = nuspecA.GetDependencyGroups().Single().Packages.Single(e => e.Id == "b");
                var dependencyBString = dependencyB.VersionRange.ToLegacyShortString();

                // Assert
                Assert.Equal(0, exitCode);

                Assert.Equal("6.0.0", dependencyBString);
            }
        }

        [Fact]
        public async Task Command_ReleaseCommand_NewMaxVersionNonInclusive_SwitchesMode()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackageA = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "6.0.0"
                    }
                };

                var depGroup = new PackageDependencyGroup(NuGetFramework.Parse("net45"), new[] {
                    new PackageDependency("b", VersionRange.Parse("(, 6.0.0)"))
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup);

                var testPackageB = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "1.0.0"
                    }
                };

                var zipFileA = testPackageA.Save(workingDir.Root);
                var zipFileB = testPackageB.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "release", workingDir.Root, "-n", "6.0.0" }, log);

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.6.0.0.nupkg"));
                var dependencyB = nuspecA.GetDependencyGroups().Single().Packages.Single(e => e.Id == "b");
                var dependencyBString = dependencyB.VersionRange.ToLegacyShortString();

                // Assert
                Assert.Equal(0, exitCode);

                Assert.Equal("(, 6.0.0]", dependencyBString);
            }
        }

        [Fact]
        public async Task Command_ReleaseCommand_NewVersionOutsideOfOriginalRange_DoubleSided()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackageA = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "6.0.0"
                    }
                };

                var depGroup = new PackageDependencyGroup(NuGetFramework.Parse("net45"), new[] {
                    new PackageDependency("b", VersionRange.Parse("[1.0.0, 2.0.0]"))
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup);

                var testPackageB = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "1.5.0"
                    }
                };

                var zipFileA = testPackageA.Save(workingDir.Root);
                var zipFileB = testPackageB.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "release", workingDir.Root, "-n", "9.0.0" }, log);

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.9.0.0.nupkg"));
                var dependencyB = nuspecA.GetDependencyGroups().Single().Packages.Single(e => e.Id == "b");
                var dependencyBString = dependencyB.VersionRange.ToLegacyShortString();

                // Assert
                Assert.Equal(0, exitCode);

                Assert.Equal("[9.0.0]", dependencyBString);
            }
        }

        [Fact]
        public async Task Command_ReleaseCommand_VersionOutsideOfOriginalRange()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackageA = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "6.0.0"
                    }
                };

                var depGroup = new PackageDependencyGroup(NuGetFramework.Parse("net45"), new[] {
                    new PackageDependency("b", VersionRange.Parse("[1.0.0, 2.0.0]"))
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup);

                var testPackageB = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "6.0.0"
                    }
                };

                var zipFileA = testPackageA.Save(workingDir.Root);
                var zipFileB = testPackageB.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "release", workingDir.Root, "-n", "9.0.0" }, log);

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.9.0.0.nupkg"));
                var dependencyB = nuspecA.GetDependencyGroups().Single().Packages.Single(e => e.Id == "b");
                var dependencyBString = dependencyB.VersionRange.ToLegacyShortString();

                // Assert
                Assert.Equal(0, exitCode);

                Assert.Equal("[1.0.0, 2.0.0]", dependencyBString);

                Assert.Contains("dependency b does not allow the original version of b 6.0.0. Skipping.", string.Join("|", log.Messages));
            }
        }

        [Fact]
        public async Task Command_ReleaseCommand_NewVersionAboveMax()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackageA = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "6.0.0"
                    }
                };

                var depGroup = new PackageDependencyGroup(NuGetFramework.Parse("net45"), new[] {
                    new PackageDependency("b", VersionRange.Parse("[ , 6.0.0]"))
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup);

                var testPackageB = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "6.0.0"
                    }
                };

                var zipFileA = testPackageA.Save(workingDir.Root);
                var zipFileB = testPackageB.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "release", workingDir.Root, "-n", "9.0.0" }, log);

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.9.0.0.nupkg"));
                var dependencyB = nuspecA.GetDependencyGroups().Single().Packages.Single(e => e.Id == "b");
                var dependencyBString = dependencyB.VersionRange.ToLegacyShortString();

                // Assert
                Assert.Equal(0, exitCode);

                Assert.Equal("(, 9.0.0]", dependencyBString);
            }
        }

        [Fact]
        public async Task Command_ReleaseCommand_NewVersionAboveMax_MatchOnMin()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackageA = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "6.0.0"
                    }
                };

                var depGroup = new PackageDependencyGroup(NuGetFramework.Parse("net45"), new[] {
                    new PackageDependency("b", VersionRange.Parse("[6.0.0 , 7.0.0]"))
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup);

                var testPackageB = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "6.0.0"
                    }
                };

                var zipFileA = testPackageA.Save(workingDir.Root);
                var zipFileB = testPackageB.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "release", workingDir.Root, "-n", "9.0.0" }, log);

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.9.0.0.nupkg"));
                var dependencyB = nuspecA.GetDependencyGroups().Single().Packages.Single(e => e.Id == "b");
                var dependencyBString = dependencyB.VersionRange.ToLegacyShortString();

                // Assert
                Assert.Equal(0, exitCode);

                Assert.Equal("[9.0.0]", dependencyBString);
            }
        }

        [Fact]
        public async Task Command_ReleaseCommand_NewVersionAboveMin()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackageA = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "6.0.0"
                    }
                };

                var depGroup = new PackageDependencyGroup(NuGetFramework.Parse("net45"), new[] {
                    new PackageDependency("b", VersionRange.Parse("5.0.0"))
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup);

                var testPackageB = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "6.0.0"
                    }
                };

                var zipFileA = testPackageA.Save(workingDir.Root);
                var zipFileB = testPackageB.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "release", workingDir.Root, "-n", "9.0.0" }, log);

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.9.0.0.nupkg"));
                var dependencyB = nuspecA.GetDependencyGroups().Single().Packages.Single(e => e.Id == "b");
                var dependencyBString = dependencyB.VersionRange.ToLegacyShortString();

                // Assert
                Assert.Equal(0, exitCode);

                Assert.Equal("9.0.0", dependencyBString);
            }
        }

        [Fact]
        public async Task Command_ReleaseCommand_NewVersionBelowMin()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackageA = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "6.0.0"
                    }
                };

                var depGroup = new PackageDependencyGroup(NuGetFramework.Parse("net45"), new[] {
                    new PackageDependency("b", VersionRange.Parse("5.0.0"))
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup);

                var testPackageB = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "6.0.0"
                    }
                };

                var zipFileA = testPackageA.Save(workingDir.Root);
                var zipFileB = testPackageB.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "release", workingDir.Root, "-n", "1.0.0" }, log);

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.1.0.0.nupkg"));
                var dependencyB = nuspecA.GetDependencyGroups().Single().Packages.Single(e => e.Id == "b");
                var dependencyBString = dependencyB.VersionRange.ToLegacyShortString();

                // Assert
                Assert.Equal(0, exitCode);

                Assert.Equal("1.0.0", dependencyBString);
            }
        }

        [Fact]
        public async Task Command_ReleaseCommand_Stable()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackageA = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0-beta.1.2"
                    }
                };

                var depGroup = new PackageDependencyGroup(NuGetFramework.Parse("net45"), new[] {
                    new PackageDependency("b", VersionRange.Parse("2.0.0-alpha")),
                    new PackageDependency("c", VersionRange.Parse("[1.0.0-beta]"))
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup);

                var testPackageB = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "2.0.0-alpha"
                    }
                };

                var testPackageC = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "c",
                        Version = "1.0.0-beta"
                    }
                };

                var zipFileA = testPackageA.Save(workingDir.Root);
                var zipFileB = testPackageB.Save(workingDir.Root);
                var zipFileC = testPackageC.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "release", workingDir.Root }, log);

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.1.0.0.nupkg"));
                var nuspecB = GetNuspec(Path.Combine(workingDir.Root, "b.2.0.0.nupkg"));
                var nuspecC = GetNuspec(Path.Combine(workingDir.Root, "c.1.0.0.nupkg"));

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Equal("1.0.0", nuspecA.GetVersion().ToString());
                Assert.Equal("2.0.0", nuspecB.GetVersion().ToString());
                Assert.Equal("1.0.0", nuspecC.GetVersion().ToString());

                Assert.Equal("2.0.0", nuspecA.GetDependencyGroups().Single().Packages.Single(e => e.Id == "b").VersionRange.ToLegacyShortString());
                Assert.Equal("[1.0.0]", nuspecA.GetDependencyGroups().Single().Packages.Single(e => e.Id == "c").VersionRange.ToLegacyShortString());
            }
        }

        [Fact]
        public async Task Command_ReleaseCommand_FourPartVersion()
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

                var testPackageB = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "2.0.0-alpha"
                    }
                };

                var testPackageC = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "c",
                        Version = "1.2.3.4"
                    }
                };

                var zipFileA = testPackageA.Save(workingDir.Root);
                var zipFileB = testPackageB.Save(workingDir.Root);
                var zipFileC = testPackageC.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "release", workingDir.Root, "--four-part-version" }, log);

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.1.0.0.0.nupkg"));
                var nuspecB = GetNuspec(Path.Combine(workingDir.Root, "b.2.0.0.0-alpha.nupkg"));
                var nuspecC = GetNuspec(Path.Combine(workingDir.Root, "c.1.2.3.4.nupkg"));

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Equal("1.0.0.0", nuspecA.GetVersion().ToString());
                Assert.Equal("2.0.0.0-alpha", nuspecB.GetVersion().ToString());
                Assert.Equal("1.2.3.4", nuspecC.GetVersion().ToString());
            }
        }

        [Fact]
        public async Task Command_ReleaseCommand_ChangeVersion()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackageA = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0-beta.1.2"
                    }
                };

                var depGroup = new PackageDependencyGroup(NuGetFramework.Parse("net45"), new[] {
                    new PackageDependency("b", VersionRange.Parse("2.0.0-alpha")),
                    new PackageDependency("c", VersionRange.Parse("[1.0.0-beta]"))
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup);

                var testPackageB = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "2.0.0-alpha"
                    }
                };

                var testPackageC = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "c",
                        Version = "1.0.0-beta"
                    }
                };

                var zipFileA = testPackageA.Save(workingDir.Root);
                var zipFileB = testPackageB.Save(workingDir.Root);
                var zipFileC = testPackageC.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "release", workingDir.Root, "-n", "5.1.1-delta" }, log);

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.5.1.1-delta.nupkg"));
                var nuspecB = GetNuspec(Path.Combine(workingDir.Root, "b.5.1.1-delta.nupkg"));
                var nuspecC = GetNuspec(Path.Combine(workingDir.Root, "c.5.1.1-delta.nupkg"));

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Equal("5.1.1-delta", nuspecA.GetVersion().ToString());
                Assert.Equal("5.1.1-delta", nuspecB.GetVersion().ToString());
                Assert.Equal("5.1.1-delta", nuspecC.GetVersion().ToString());

                Assert.Equal("5.1.1-delta", nuspecA.GetDependencyGroups().Single().Packages.Single(e => e.Id == "b").VersionRange.ToLegacyShortString());
                Assert.Equal("[5.1.1-delta]", nuspecA.GetDependencyGroups().Single().Packages.Single(e => e.Id == "c").VersionRange.ToLegacyShortString());
            }
        }

        [Fact]
        public async Task Command_ReleaseCommand_Label()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackageA = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0-beta.1.2"
                    }
                };

                var depGroup = new PackageDependencyGroup(NuGetFramework.Parse("net45"), new[] {
                    new PackageDependency("b", VersionRange.Parse("2.0.0-alpha")),
                    new PackageDependency("c", VersionRange.Parse("[1.0.0-beta]"))
                });

                testPackageA.Nuspec.Dependencies.Add(depGroup);

                var testPackageB = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "2.0.0-alpha"
                    }
                };

                var testPackageC = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "c",
                        Version = "1.0.0-beta"
                    }
                };

                var zipFileA = testPackageA.Save(workingDir.Root);
                var zipFileB = testPackageB.Save(workingDir.Root);
                var zipFileC = testPackageC.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "release", workingDir.Root, "--label", "rc.1" }, log);

                var nuspecA = GetNuspec(Path.Combine(workingDir.Root, "a.1.0.0-rc.1.nupkg"));
                var nuspecB = GetNuspec(Path.Combine(workingDir.Root, "b.2.0.0-rc.1.nupkg"));
                var nuspecC = GetNuspec(Path.Combine(workingDir.Root, "c.1.0.0-rc.1.nupkg"));

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Equal("1.0.0-rc.1", nuspecA.GetVersion().ToString());
                Assert.Equal("2.0.0-rc.1", nuspecB.GetVersion().ToString());
                Assert.Equal("1.0.0-rc.1", nuspecC.GetVersion().ToString());

                Assert.Equal("2.0.0-rc.1", nuspecA.GetDependencyGroups().Single().Packages.Single(e => e.Id == "b").VersionRange.ToLegacyShortString());
                Assert.Equal("[1.0.0-rc.1]", nuspecA.GetDependencyGroups().Single().Packages.Single(e => e.Id == "c").VersionRange.ToLegacyShortString());
            }
        }

        [Fact]
        public async Task Command_ReleaseCommand_CollisionOnNewVersion()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackageA1 = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var testPackageA2 = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "2.0.0"
                    }
                };

                var zipFileA1 = testPackageA1.Save(workingDir.Root);
                var zipFileA2 = testPackageA2.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "release", workingDir.Root, "-n", "3.0.0" }, log);

                // Assert
                Assert.Equal(1, exitCode);
                Assert.Contains("Output file name collision on", log.GetMessages());
            }
        }

        [Fact]
        public async Task Command_ReleaseCommand_NoCollisionOnNewVersionWhenSymbols()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackageA1 = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var zipFileA1 = testPackageA1.Save(workingDir.Root);

                var symbolsPath = zipFileA1.FullName.Replace(".nupkg", ".symbols.nupkg");
                File.Copy(zipFileA1.FullName, symbolsPath);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "release", workingDir.Root, "-n", "3.0.0" }, log);

                // Assert
                Assert.Equal(0, exitCode);
                var nuspecA1 = GetNuspec(Path.Combine(workingDir.Root, "a.3.0.0.nupkg"));
                var nuspecA2 = GetNuspec(Path.Combine(workingDir.Root, "a.3.0.0.symbols.nupkg"));

                Assert.Equal("3.0.0", nuspecA1.GetVersion().ToNormalizedString());
                Assert.Equal("3.0.0", nuspecA2.GetVersion().ToNormalizedString());
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