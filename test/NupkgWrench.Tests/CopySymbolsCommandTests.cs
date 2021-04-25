using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using NuGet.Packaging;
using NuGet.Test.Helpers;
using Xunit;

namespace NupkgWrench.Tests
{
    public class CopySymbolsCommandTests
    {
        [Fact]
        public async Task GivenThatICopySymbolsVerifyFirstSymbolPackageUsed()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var log = new TestLogger();
                var nuspec = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0.0"
                };
                var nupkg = nuspec.CreateNupkg();
                nupkg.Files.Clear();
                nupkg.AddFile("lib/net45/a.dll");
                var path = nupkg.Save(workingDir);

                var nuspecSymbols = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0.0",
                    IsSymbolPackage = true
                };
                var nupkgSymbols = nuspecSymbols.CreateNupkg();
                nupkgSymbols.Files.Clear();
                nupkgSymbols.AddFile("lib/net45/a.dll");
                nupkgSymbols.AddFile("lib/net45/a.pdb");
                nupkgSymbols.Save(workingDir);

                var nuspecSymbols2 = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0",
                    IsSymbolPackage = true
                };
                var nupkgSymbols2 = nuspecSymbols2.CreateNupkg();
                nupkgSymbols2.Files.Clear();
                nupkgSymbols2.AddFile("lib/net45/a.dll");
                nupkgSymbols2.Save(workingDir);

                // Act
                var before = GetNupkgFiles(path.FullName);
                var exitCode = await Program.MainCore(new[] { "files", "copysymbols", workingDir }, log);
                var after = GetNupkgFiles(path.FullName);

                var diff = new SortedSet<string>(after, StringComparer.OrdinalIgnoreCase);
                diff.ExceptWith(before);

                // Assert
                exitCode.Should().Be(0, log.GetMessages());

                after.Should().Contain("lib/net45/a.dll");
                diff.ShouldBeEquivalentTo(new string[] { "lib/net45/a.pdb" });
            }
        }

        [Fact]
        public async Task GivenThatICopySymbolsVerifyNoMismatchedSymbolsNotAdded()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var log = new TestLogger();
                var nuspec = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0.0"
                };
                var nupkg = nuspec.CreateNupkg();
                nupkg.Files.Clear();
                nupkg.AddFile("lib/net45/a.dll");
                var path = nupkg.Save(workingDir);

                var nuspecSymbols = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0.0",
                    IsSymbolPackage = true
                };
                var nupkgSymbols = nuspecSymbols.CreateNupkg();
                nupkgSymbols.Files.Clear();
                nupkgSymbols.AddFile("lib/net45/a.dll");
                nupkgSymbols.AddFile("lib/net45/b.pdb");
                nupkgSymbols.Save(workingDir);

                // Act
                var before = GetNupkgFiles(path.FullName);
                var exitCode = await Program.MainCore(new[] { "files", "copysymbols", workingDir }, log);
                var after = GetNupkgFiles(path.FullName);

                var diff = new SortedSet<string>(after, StringComparer.OrdinalIgnoreCase);
                diff.ExceptWith(before);

                // Assert
                exitCode.Should().Be(0, log.GetMessages());

                after.Should().Contain("lib/net45/a.dll");
                diff.ShouldBeEquivalentTo(new string[] { });
            }
        }

        [Fact]
        public async Task GivenThatICopySymbolsVerifyTheyAreAddedToTheNupkg()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var log = new TestLogger();
                var nuspec = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0.0"
                };
                var nupkg = nuspec.CreateNupkg();
                nupkg.Files.Clear();
                nupkg.AddFile("lib/net45/a.dll");
                var path = nupkg.Save(workingDir);

                var nuspecSymbols = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0.0",
                    IsSymbolPackage = true
                };
                var nupkgSymbols = nuspecSymbols.CreateNupkg();
                nupkgSymbols.Files.Clear();
                nupkgSymbols.AddFile("lib/net45/a.dll");
                nupkgSymbols.AddFile("lib/net45/a.pdb");
                nupkgSymbols.Save(workingDir);

                // Act
                var before = GetNupkgFiles(path.FullName);
                var exitCode = await Program.MainCore(new[] { "files", "copysymbols", workingDir }, log);
                var after = GetNupkgFiles(path.FullName);

                var diff = new SortedSet<string>(after, StringComparer.OrdinalIgnoreCase);
                diff.ExceptWith(before);

                // Assert
                exitCode.Should().Be(0, log.GetMessages());

                after.Should().Contain("lib/net45/a.dll");
                diff.ShouldBeEquivalentTo(new[] { "lib/net45/a.pdb" });
            }
        }

        [Fact]
        public async Task GivenThatICopySymbolsAndTheyAlreadyExistVerifyNoErrors()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var log = new TestLogger();
                var nuspec = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0.0"
                };
                var nupkg = nuspec.CreateNupkg();
                nupkg.Files.Clear();
                nupkg.AddFile("lib/net45/a.dll");
                nupkg.AddFile("lib/net45/a.pdb");
                var path = nupkg.Save(workingDir);

                var nuspecSymbols = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0.0",
                    IsSymbolPackage = true
                };
                var nupkgSymbols = nuspecSymbols.CreateNupkg();
                nupkgSymbols.Files.Clear();
                nupkgSymbols.AddFile("lib/net45/a.dll");
                nupkgSymbols.AddFile("lib/net45/a.pdb");
                nupkgSymbols.Save(workingDir);

                // Act
                var before = GetNupkgFiles(path.FullName);
                var exitCode = await Program.MainCore(new[] { "files", "copysymbols", workingDir }, log);
                var after = GetNupkgFiles(path.FullName);

                var diff = new SortedSet<string>(after, StringComparer.OrdinalIgnoreCase);
                diff.ExceptWith(before);

                // Assert
                exitCode.Should().Be(0, log.GetMessages());

                after.Should().Contain("lib/net45/a.dll");
                after.Should().Contain("lib/net45/a.pdb");
            }
        }

        [Fact]
        public async Task GivenThatICopySymbolsWithNoSymbolsVerifyWarning()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var log = new TestLogger();
                var nuspec = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0.0"
                };
                var nupkg = nuspec.CreateNupkg();
                nupkg.Files.Clear();
                nupkg.AddFile("lib/net45/a.dll");
                var path = nupkg.Save(workingDir);

                // Act
                var before = GetNupkgFiles(path.FullName);
                var exitCode = await Program.MainCore(new[] { "files", "copysymbols", workingDir }, log);
                var after = GetNupkgFiles(path.FullName);

                var diff = new SortedSet<string>(after, StringComparer.OrdinalIgnoreCase);
                diff.ExceptWith(before);

                // Assert
                exitCode.Should().Be(0, log.GetMessages());

                log.GetMessages().Should().Contain("Missing symbols package for a.1.0.0");
            }
        }

        [Fact]
        public async Task GivenThatICopySymbolsWithNoPrimaryVerifyWarning()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var log = new TestLogger();
                var nuspec = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0.0",
                    IsSymbolPackage = true
                };
                var nupkg = nuspec.CreateNupkg();
                nupkg.Files.Clear();
                nupkg.AddFile("lib/net45/a.dll");
                var path = nupkg.Save(workingDir);

                // Act
                var before = GetNupkgFiles(path.FullName);
                var exitCode = await Program.MainCore(new[] { "files", "copysymbols", workingDir }, log);
                var after = GetNupkgFiles(path.FullName);

                var diff = new SortedSet<string>(after, StringComparer.OrdinalIgnoreCase);
                diff.ExceptWith(before);

                // Assert
                exitCode.Should().Be(0, log.GetMessages());

                log.GetMessages().Should().Be("Missing primary package for a.1.0.0");
            }
        }

        [Fact]
        public async Task GivenThatICopySymbolsWithNoFilesVerifyNoErrors()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "files", "copysymbols", workingDir }, log);

                // Assert
                exitCode.Should().Be(0, log.GetMessages());
            }
        }

        private static SortedSet<string> GetNupkgFiles(string path)
        {
            using (var reader = new PackageArchiveReader(path))
            {
                return new SortedSet<string>(reader.GetFiles(), StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
