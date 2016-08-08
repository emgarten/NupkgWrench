using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using Xunit;

namespace NupkgWrench.Tests
{
    public class CommandTests
    {
        [Fact]
        public async Task Command_IdCommand()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var zipFile = testPackage.Create(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "id", zipFile.FullName }, log);

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Equal("a", string.Join("|", log.Messages));
            }
        }

        [Fact]
        public async Task Command_VersionCommand()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
                    {
                        Id = "a",
                        Version = "1.0.0-beta.1.2"
                    }
                };

                var zipFile = testPackage.Create(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "version", zipFile.FullName }, log);

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Equal("1.0.0-beta.1.2", string.Join("|", log.Messages));
            }
        }

        [Fact]
        public async Task Command_ListCommandNoFilters()
        {
            using (var workingDir = new TestFolder())
            using (var workingDir2 = new TestFolder())
            using (var workingDir3 = new TestFolder())
            {
                // Arrange
                var testPackageA = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
                    {
                        Id = "a",
                        Version = "1.0.0-beta.2.2"
                    }
                };

                var testPackageB = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
                    {
                        Id = "b",
                        Version = "1.0.0-beta.1.2"
                    }
                };

                var testPackageC = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
                    {
                        Id = "c",
                        Version = "2.0.0"
                    }
                };

                var zipFileA = testPackageA.Create(workingDir.Root);
                var zipFileB = testPackageB.Create(workingDir2.Root);
                var zipFileC = testPackageC.Create(workingDir3.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "list", workingDir.Root, workingDir2.Root, zipFileC.FullName }, log);

                var files = log.Messages.Select(e => Path.GetFileName(e)).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Equal(3, files.Count);
                Assert.Equal("a.1.0.0-beta.2.2.nupkg", files[0]);
                Assert.Equal("b.1.0.0-beta.1.2.nupkg", files[1]);
                Assert.Equal("c.2.0.0.nupkg", files[2]);
            }
        }

        [Fact]
        public async Task Command_ListCommand_Id()
        {
            using (var workingDir = new TestFolder())
            using (var workingDir2 = new TestFolder())
            using (var workingDir3 = new TestFolder())
            {
                // Arrange
                var testPackageA = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
                    {
                        Id = "a",
                        Version = "1.0.0-beta.2.2"
                    }
                };

                var testPackageB = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
                    {
                        Id = "b",
                        Version = "1.0.0-beta.1.2"
                    }
                };

                var testPackageC = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
                    {
                        Id = "c",
                        Version = "2.0.0"
                    }
                };

                var zipFileA = testPackageA.Create(workingDir.Root);
                var zipFileB = testPackageB.Create(workingDir2.Root);
                var zipFileC = testPackageC.Create(workingDir3.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "list", workingDir.Root, workingDir2.Root, zipFileC.FullName, "--id", "a" }, log);

                var files = log.Messages.Select(e => Path.GetFileName(e)).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Equal(1, files.Count);
                Assert.Equal("a.1.0.0-beta.2.2.nupkg", files[0]);
            }
        }

        [Fact]
        public async Task Command_ListCommand_IdNoMatch()
        {
            using (var workingDir = new TestFolder())
            using (var workingDir2 = new TestFolder())
            using (var workingDir3 = new TestFolder())
            {
                // Arrange
                var testPackageA = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
                    {
                        Id = "a",
                        Version = "1.0.0-beta.2.2"
                    }
                };

                var testPackageB = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
                    {
                        Id = "b",
                        Version = "1.0.0-beta.1.2"
                    }
                };

                var testPackageC = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
                    {
                        Id = "c",
                        Version = "2.0.0"
                    }
                };

                var zipFileA = testPackageA.Create(workingDir.Root);
                var zipFileB = testPackageB.Create(workingDir2.Root);
                var zipFileC = testPackageC.Create(workingDir3.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "list", workingDir.Root, workingDir2.Root, zipFileC.FullName, "--id", "a*d*" }, log);

                var files = log.Messages.Select(e => Path.GetFileName(e)).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Equal(0, files.Count);
            }
        }

        [Fact]
        public async Task Command_ListCommand_Version()
        {
            using (var workingDir = new TestFolder())
            using (var workingDir2 = new TestFolder())
            using (var workingDir3 = new TestFolder())
            {
                // Arrange
                var testPackageA = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
                    {
                        Id = "a",
                        Version = "1.0.0-beta.2.2"
                    }
                };

                var testPackageB = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
                    {
                        Id = "b",
                        Version = "1.0.0-beta.1.2"
                    }
                };

                var testPackageC = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
                    {
                        Id = "c",
                        Version = "2.0.0"
                    }
                };

                var zipFileA = testPackageA.Create(workingDir.Root);
                var zipFileB = testPackageB.Create(workingDir2.Root);
                var zipFileC = testPackageC.Create(workingDir3.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "list", workingDir.Root, workingDir2.Root, zipFileC.FullName, "--version", "1.0.0-beta.*.2" }, log);

                var files = log.Messages.Select(e => Path.GetFileName(e)).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Equal(2, files.Count);
                Assert.Equal("a.1.0.0-beta.2.2.nupkg", files[0]);
                Assert.Equal("b.1.0.0-beta.1.2.nupkg", files[1]);
            }
        }

        [Fact]
        public async Task Command_UpdateFileNameCommand_Noop()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var zipFile = testPackage.Create(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "updatefilename", zipFile.FullName }, log);

                // Assert
                Assert.Equal(0, exitCode);
                Assert.True(File.Exists(zipFile.FullName));
            }
        }

        [Fact]
        public async Task Command_UpdateFileNameCommand()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var zipFile = testPackage.Create(workingDir.Root);

                var log = new TestLogger();

                var altPath = Path.Combine(zipFile.Directory.FullName, "test.nupkg");
                var origPath = zipFile.FullName;

                zipFile.MoveTo(altPath);

                // Act
                var exitCode = await Program.MainCore(new[] { "updatefilename", altPath }, log);

                // Assert
                Assert.Equal(0, exitCode);
                Assert.True(File.Exists(origPath), string.Join("|", log.Messages));
            }
        }

        [Fact]
        public async Task Command_ValidateCommand()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var zipFile = testPackage.Create(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "validate", workingDir.Root }, log);

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Contains("valid", string.Join("|", log.Messages));
            }
        }

        [Fact]
        public async Task Command_ValidateCommand_Invalid()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
                    {
                        Id = "a",
                        Version = "b"
                    }
                };

                var zipFile = testPackage.Create(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "validate", workingDir.Root }, log);

                // Assert
                Assert.Equal(1, exitCode);
                Assert.Contains("error", string.Join("|", log.Messages));
            }
        }

        [Fact]
        public async Task Command_ReleaseCommand_Stable()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackageA = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
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

                var testPackageB = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
                    {
                        Id = "b",
                        Version = "2.0.0-alpha"
                    }
                };

                var testPackageC = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
                    {
                        Id = "c",
                        Version = "1.0.0-beta"
                    }
                };

                var zipFileA = testPackageA.Create(workingDir.Root);
                var zipFileB = testPackageB.Create(workingDir.Root);
                var zipFileC = testPackageC.Create(workingDir.Root);

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
        public async Task Command_ReleaseCommand_ChangeVersion()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackageA = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
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

                var testPackageB = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
                    {
                        Id = "b",
                        Version = "2.0.0-alpha"
                    }
                };

                var testPackageC = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
                    {
                        Id = "c",
                        Version = "1.0.0-beta"
                    }
                };

                var zipFileA = testPackageA.Create(workingDir.Root);
                var zipFileB = testPackageB.Create(workingDir.Root);
                var zipFileC = testPackageC.Create(workingDir.Root);

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
                var testPackageA = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
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

                var testPackageB = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
                    {
                        Id = "b",
                        Version = "2.0.0-alpha"
                    }
                };

                var testPackageC = new TestPackageContext()
                {
                    Nuspec = new TestNuspecContext()
                    {
                        Id = "c",
                        Version = "1.0.0-beta"
                    }
                };

                var zipFileA = testPackageA.Create(workingDir.Root);
                var zipFileB = testPackageB.Create(workingDir.Root);
                var zipFileC = testPackageC.Create(workingDir.Root);

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

        private static NuspecReader GetNuspec(string path)
        {
            using (var reader = new PackageArchiveReader(path))
            {
                return reader.NuspecReader;
            }
        }
    }
}
