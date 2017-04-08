using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
    public class CommandTests
    {
        [Fact]
        public async Task Command_IdCommand()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var zipFile = testPackage.Save(workingDir.Root);

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
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0-beta.1.2"
                    }
                };

                var zipFile = testPackage.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "version", zipFile.FullName }, log);

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Equal("1.0.0-beta.1.2", string.Join("|", log.Messages));
            }
        }

        [Fact]
        public async Task Command_VersionCommand_MatchOnDirectory()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0-beta.1.2"
                    }
                };

                var zipFile = testPackage.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "version", workingDir.Root }, log);

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Equal("1.0.0-beta.1.2", string.Join("|", log.Messages));
            }
        }

        [Fact]
        public async Task Command_VersionCommand_HighestFilter()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage1 = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0-beta.1.2"
                    }
                };

                var testPackage2 = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0-beta.2.2"
                    }
                };

                testPackage1.Save(workingDir.Root);
                testPackage2.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "version", workingDir.Root, "--highest-version" }, log);

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Equal("1.0.0-beta.2.2", string.Join("|", log.Messages));
            }
        }

        [Fact]
        public async Task Command_VersionCommand_FailsWhenMultipleMatch()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage1 = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0-beta.1.2"
                    }
                };

                var testPackage2 = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0-beta.2.2"
                    }
                };

                testPackage1.Save(workingDir.Root);
                testPackage2.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "version", workingDir.Root }, log);

                // Assert
                Assert.Equal(1, exitCode);
                Assert.Contains("The input filters given match multiple nupkgs", string.Join("|", log.Messages));
            }
        }

        [Fact]
        public async Task Command_VersionCommand_FailsWhenZeroMatch()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "version", workingDir.Root }, log);

                // Assert
                Assert.Equal(1, exitCode);
                Assert.Contains("The input filters given match zero nupkgs", string.Join("|", log.Messages));
            }
        }

        [Fact]
        public async Task Command_VersionCommand_FilterByIdToSingleMatch()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage1 = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0-beta.1.2"
                    }
                };

                var testPackage2 = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "aa",
                        Version = "1.0.0-beta.2.2"
                    }
                };

                testPackage1.Save(workingDir.Root);
                testPackage2.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "version", workingDir.Root, "--id", "a" }, log);

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Contains("1.0.0-beta.1.2", string.Join("|", log.Messages));
            }
        }

        [Fact]
        public async Task Command_VersionCommand_FilterByIdToSingleMatchExcludeSymbols()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage1 = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0-beta.1.2"
                    }
                };

                var testPackage2 = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "aa",
                        Version = "1.0.0-beta.2.2"
                    }
                };

                testPackage1.Save(workingDir.Root);
                testPackage2.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "version", workingDir.Root, "--id", "a", "--exclude-symbols" }, log);

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Contains("1.0.0-beta.1.2", string.Join("|", log.Messages));
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
                var testPackageA = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0-beta.2.2"
                    }
                };

                var testPackageB = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "1.0.0-beta.1.2"
                    }
                };

                var testPackageC = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "c",
                        Version = "2.0.0"
                    }
                };

                var zipFileA = testPackageA.Save(workingDir.Root);
                var zipFileB = testPackageB.Save(workingDir2.Root);
                var zipFileC = testPackageC.Save(workingDir3.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "list", workingDir.Root, workingDir2.Root, zipFileC.FullName }, log);

                var files = log.Messages.Select(e => Path.GetFileName(e.Message)).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Equal(3, files.Count);
                Assert.Equal("a.1.0.0-beta.2.2.nupkg", files[0]);
                Assert.Equal("b.1.0.0-beta.1.2.nupkg", files[1]);
                Assert.Equal("c.2.0.0.nupkg", files[2]);
            }
        }

        [Fact]
        public async Task Command_ListCommand_GlobbingPath()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackageA = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0-beta.2.2"
                    }
                };

                var testPackageB = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "1.0.0-beta.1.2"
                    }
                };

                var testPackageC = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "c",
                        Version = "2.0.0"
                    }
                };

                var subFolder = Path.Combine(workingDir.Root, "subFolder");

                var zipFileA = testPackageA.Save(subFolder);
                var zipFileB = testPackageB.Save(subFolder);
                var zipFileC = testPackageC.Save(subFolder);

                var log = new TestLogger();

                var input = workingDir.Root + Path.DirectorySeparatorChar + "**/*.nupkg";

                // Act
                var exitCode = await Program.MainCore(new[] { "list", input }, log);

                var files = log.Messages.Select(e => Path.GetFileName(e.Message)).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();

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
                var testPackageA = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0-beta.2.2"
                    }
                };

                var testPackageB = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "1.0.0-beta.1.2"
                    }
                };

                var testPackageC = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "c",
                        Version = "2.0.0"
                    }
                };

                var zipFileA = testPackageA.Save(workingDir.Root);
                var zipFileB = testPackageB.Save(workingDir2.Root);
                var zipFileC = testPackageC.Save(workingDir3.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "list", workingDir.Root, workingDir2.Root, zipFileC.FullName, "--id", "a" }, log);

                var files = log.Messages.Select(e => Path.GetFileName(e.Message)).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();

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
                var testPackageA = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0-beta.2.2"
                    }
                };

                var testPackageB = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "1.0.0-beta.1.2"
                    }
                };

                var testPackageC = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "c",
                        Version = "2.0.0"
                    }
                };

                var zipFileA = testPackageA.Save(workingDir.Root);
                var zipFileB = testPackageB.Save(workingDir2.Root);
                var zipFileC = testPackageC.Save(workingDir3.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "list", workingDir.Root, workingDir2.Root, zipFileC.FullName, "--id", "a*d*" }, log);

                var files = log.Messages.Select(e => Path.GetFileName(e.Message)).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Equal(0, files.Count);
            }
        }

        [Fact]
        public async Task Command_ListCommand_NoMatch_VerifyNoOutput()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "list", workingDir.Root }, log);

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Equal(0, log.Messages.Count);
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
                var testPackageA = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0-beta.2.2"
                    }
                };

                var testPackageB = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "b",
                        Version = "1.0.0-beta.1.2"
                    }
                };

                var testPackageC = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "c",
                        Version = "2.0.0"
                    }
                };

                var zipFileA = testPackageA.Save(workingDir.Root);
                var zipFileB = testPackageB.Save(workingDir2.Root);
                var zipFileC = testPackageC.Save(workingDir3.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "list", workingDir.Root, workingDir2.Root, zipFileC.FullName, "--version", "1.0.0-beta.*.2" }, log);

                var files = log.Messages.Select(e => Path.GetFileName(e.Message)).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Equal(2, files.Count);
                Assert.Equal("a.1.0.0-beta.2.2.nupkg", files[0]);
                Assert.Equal("b.1.0.0-beta.1.2.nupkg", files[1]);
            }
        }

        [Fact]
        public async Task Command_ValidateCommand()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var zipFile = testPackage.Save(workingDir.Root);

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
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "b"
                    }
                };

                var zipFile = testPackage.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "validate", workingDir.Root }, log);

                // Assert
                Assert.Equal(1, exitCode);
                Assert.Contains("error", string.Join("|", log.Messages));
            }
        }

        [Fact]
        public async Task Command_AddFilesCommand()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var inputFile = Path.Combine(workingDir.Root, "test.dll");
                File.WriteAllText(inputFile, "a");

                var zipFile = testPackage.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "files", "add", zipFile.FullName, "-p", "lib/win8/test.dll", "-f", inputFile }, log);

                var files = GetFiles(zipFile.FullName);

                // Assert
                Assert.True(0 == exitCode, string.Join("|", log.Messages));
                Assert.Contains("lib/win8/test.dll", string.Join("|", files));
            }
        }

        [Fact]
        public async Task Command_AddFilesCommand_Twice()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var inputFile = Path.Combine(workingDir.Root, "test.dll");
                File.WriteAllText(inputFile, "a");

                var zipFile = testPackage.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "files", "add", zipFile.FullName, "-p", "lib/win8/test.dll", "-f", inputFile }, log);
                exitCode = await Program.MainCore(new[] { "files", "add", zipFile.FullName, "-p", "lib/win8/test.dll", "-f", inputFile }, log);

                var files = GetFiles(zipFile.FullName);

                // Assert
                Assert.True(0 == exitCode, string.Join("|", log.Messages));
                Assert.Contains("lib/win8/test.dll", string.Join("|", files));
                Assert.Contains("removing", string.Join("|", log.Messages));
            }
        }

        [Fact]
        public async Task Command_AddFilesCommand_Remove()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var inputFile = Path.Combine(workingDir.Root, "test.dll");
                File.WriteAllText(inputFile, "a");

                var zipFile = testPackage.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "files", "add", zipFile.FullName, "-p", "lib/win8/test.dll", "-f", inputFile }, log);
                exitCode = await Program.MainCore(new[] { "files", "remove", zipFile.FullName, "-p", "lib/win8/test.dll" }, log);

                var files = GetFiles(zipFile.FullName);

                // Assert
                Assert.True(0 == exitCode, string.Join("|", log.Messages));
                Assert.DoesNotContain("lib/win8/test.dll", string.Join("|", files));
                Assert.Contains("removing", string.Join("|", log.Messages));
            }
        }

        [Fact]
        public async Task Command_ListFilesCommand()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var inputFile = Path.Combine(workingDir.Root, "test.dll");
                File.WriteAllText(inputFile, "a");

                var zipFile = testPackage.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "files", "add", zipFile.FullName, "-p", "lib/win8/test.dll", "-f", inputFile }, log);
                exitCode = await Program.MainCore(new[] { "files", "list", zipFile.FullName }, log);

                var files = GetFiles(zipFile.FullName);

                // Assert
                Assert.True(0 == exitCode, string.Join("|", log.Messages));

                foreach (var file in files)
                {
                    Assert.Contains(file, string.Join("|", log.Messages));
                }
            }
        }

        [Fact]
        public async Task Command_ContentFilesCommand()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var zipFile = testPackage.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "contentfiles", "add", zipFile.FullName, "--include", "**/*.*", "--exclude", "**/*.txt", "--build-action", "none", "--copy-to-output", "true", "--flatten", "true" }, log);
                var nuspec = GetNuspec(zipFile.FullName);

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Equal("none", nuspec.GetContentFiles().Single().BuildAction);
                Assert.Equal(true, nuspec.GetContentFiles().Single().CopyToOutput);
                Assert.Equal("**/*.txt", nuspec.GetContentFiles().Single().Exclude);
                Assert.Equal("**/*.*", nuspec.GetContentFiles().Single().Include);
                Assert.Equal(true, nuspec.GetContentFiles().Single().Flatten);
            }
        }

        [Fact]
        public async Task Command_ContentFilesCommand_Multiple()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var zipFile = testPackage.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "contentfiles", "add", zipFile.FullName, "--include", "**/*.*" }, log);
                exitCode = await Program.MainCore(new[] { "nuspec", "contentfiles", "add", zipFile.FullName, "--include", "**/*.txt" }, log);
                var nuspec = GetNuspec(zipFile.FullName);

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Equal(2, nuspec.GetContentFiles().Count());
            }
        }

        [Fact]
        public async Task Command_DependenciesClearCommand()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var depGroup1 = new PackageDependencyGroup(NuGetFramework.Parse("net45"), new[] {
                    new PackageDependency("b", VersionRange.Parse("2.0.0-alpha")),
                    new PackageDependency("c", VersionRange.Parse("[1.0.0-beta]"))
                });

                var depGroup2 = new PackageDependencyGroup(NuGetFramework.Parse("netstandard1.6"), new[] {
                    new PackageDependency("b", VersionRange.Parse("2.0.0-alpha")),
                    new PackageDependency("c", VersionRange.Parse("[1.0.0-beta]"))
                });

                testPackage.Nuspec.Dependencies.Add(depGroup1);
                testPackage.Nuspec.Dependencies.Add(depGroup2);

                var zipFile = testPackage.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "clear", zipFile.FullName }, log);
                var nuspec = GetNuspec(zipFile.FullName);

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Equal(0, nuspec.GetDependencyGroups().Count());
            }
        }

        [Fact]
        public async Task Command_DependenciesClearCommand_Filter()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var depGroup1 = new PackageDependencyGroup(NuGetFramework.Parse("net45"), new[] {
                    new PackageDependency("b", VersionRange.Parse("2.0.0-alpha")),
                    new PackageDependency("c", VersionRange.Parse("[1.0.0-beta]"))
                });

                var depGroup2 = new PackageDependencyGroup(NuGetFramework.Parse("netstandard1.6"), new[] {
                    new PackageDependency("b", VersionRange.Parse("2.0.0-alpha")),
                    new PackageDependency("c", VersionRange.Parse("[1.0.0-beta]"))
                });

                testPackage.Nuspec.Dependencies.Add(depGroup1);
                testPackage.Nuspec.Dependencies.Add(depGroup2);

                var zipFile = testPackage.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "clear", zipFile.FullName, "-f", "net45" }, log);
                var nuspec = GetNuspec(zipFile.FullName);

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Equal(1, nuspec.GetDependencyGroups().Count());
            }
        }

        [Fact]
        public async Task Command_DependenciesEmptyGroupCommand()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var depGroup1 = new PackageDependencyGroup(NuGetFramework.Parse("net45"), new[] {
                    new PackageDependency("b", VersionRange.Parse("2.0.0-alpha")),
                    new PackageDependency("c", VersionRange.Parse("[1.0.0-beta]"))
                });

                var depGroup2 = new PackageDependencyGroup(NuGetFramework.Parse("netstandard1.6"), new[] {
                    new PackageDependency("b", VersionRange.Parse("2.0.0-alpha")),
                    new PackageDependency("c", VersionRange.Parse("[1.0.0-beta]"))
                });

                testPackage.Nuspec.Dependencies.Add(depGroup1);
                testPackage.Nuspec.Dependencies.Add(depGroup2);

                var zipFile = testPackage.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "emptygroup", zipFile.FullName, "-f", "net45" }, log);
                var nuspec = GetNuspec(zipFile.FullName);

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Equal(2, nuspec.GetDependencyGroups().Count());
                Assert.Equal(0, nuspec.GetDependencyGroups().Where(e => e.TargetFramework.Equals(NuGetFramework.Parse("net45"))).Single().Packages.Count());
                Assert.Equal(2, nuspec.GetDependencyGroups().Where(e => e.TargetFramework.Equals(NuGetFramework.Parse("netstandard1.6"))).Single().Packages.Count());
            }
        }

        [Fact]
        public async Task Command_DependenciesEmptyGroupCommand_AddNew()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var depGroup1 = new PackageDependencyGroup(NuGetFramework.Parse("net45"), new[] {
                    new PackageDependency("b", VersionRange.Parse("2.0.0-alpha")),
                    new PackageDependency("c", VersionRange.Parse("[1.0.0-beta]"))
                });

                var depGroup2 = new PackageDependencyGroup(NuGetFramework.Parse("netstandard1.6"), new[] {
                    new PackageDependency("b", VersionRange.Parse("2.0.0-alpha")),
                    new PackageDependency("c", VersionRange.Parse("[1.0.0-beta]"))
                });

                testPackage.Nuspec.Dependencies.Add(depGroup1);
                testPackage.Nuspec.Dependencies.Add(depGroup2);

                var zipFile = testPackage.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "dependencies", "emptygroup", zipFile.FullName, "-f", "win8" }, log);
                var nuspec = GetNuspec(zipFile.FullName);

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Equal(3, nuspec.GetDependencyGroups().Count());
                Assert.Equal(2, nuspec.GetDependencyGroups().Where(e => e.TargetFramework.Equals(NuGetFramework.Parse("net45"))).Single().Packages.Count());
                Assert.Equal(2, nuspec.GetDependencyGroups().Where(e => e.TargetFramework.Equals(NuGetFramework.Parse("netstandard1.6"))).Single().Packages.Count());
                Assert.Equal(0, nuspec.GetDependencyGroups().Where(e => e.TargetFramework.Equals(NuGetFramework.Parse("win8"))).Single().Packages.Count());
            }
        }

        [Fact]
        public async Task Command_FrameworkAssembliesClear()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                testPackage.Nuspec.FrameworkAssemblies.Add(new KeyValuePair<string, List<NuGetFramework>>("test", new List<NuGetFramework>() { NuGetFramework.Parse("net45") }));

                var zipFile = testPackage.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "frameworkassemblies", "clear", zipFile.FullName }, log);
                var nuspec = GetNuspec(zipFile.FullName);

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Equal(0, nuspec.GetFrameworkReferenceGroups().Count());
            }
        }

        [Fact]
        public async Task Command_NuspecShowCommand()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var zipFile = testPackage.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "show", zipFile.FullName }, log);

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Contains("<version>1.0.0</version>", string.Join("|", log.Messages));
            }
        }

        [Fact]
        public async Task Command_NuspecEditCommand()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var testPackage = new TestNupkg()
                {
                    Nuspec = new TestNuspec()
                    {
                        Id = "a",
                        Version = "1.0.0"
                    }
                };

                var zipFile = testPackage.Save(workingDir.Root);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "edit", zipFile.FullName, "-p", "version", "-s", "2.0.0-beta" }, log);
                exitCode += await Program.MainCore(new[] { "nuspec", "show", zipFile.FullName }, log);

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Contains("<version>2.0.0-beta</version>", string.Join("|", log.Messages));
            }
        }

        private static NuspecReader GetNuspec(string path)
        {
            using (var reader = new PackageArchiveReader(path))
            {
                return reader.NuspecReader;
            }
        }

        private static SortedSet<string> GetFiles(string path)
        {
            var files = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

            using (var stream = File.OpenRead(path))
            using (var zip = new ZipArchive(stream))
            {
                files.AddRange(zip.Entries.Select(e => e.FullName));
            }

            return files;
        }
    }
}