using System.IO;
using System.Threading.Tasks;
using AwesomeAssertions;
using NuGet.Packaging;
using NuGet.Test.Helpers;
using Xunit;

namespace NupkgWrench.Tests
{
    public class UpdateFileNameCommandTests
    {
        [Fact]
        public async Task GivenThatIUpdateASymbolPackageVerifyTheFileNameIsCorrect()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var nuspec = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0.0",
                    IsSymbolPackage = true
                };

                var nupkg = nuspec.CreateNupkg().Save(workingDir);

                var log = new TestLogger();

                var altPath = Path.Combine(nupkg.Directory.FullName, "a.1.0.symbols.nupkg");
                var origPath = nupkg.FullName;

                File.Move(origPath, altPath);

                // Act
                var exitCode = await Program.MainCore(new[] { "updatefilename", altPath }, log);

                // Assert
                exitCode.Should().Be(0, "no errors");
                File.Exists(origPath).Should().BeTrue("the original path was the correct name");
            }
        }

        [Fact]
        public async Task GivenThatIUpdateAnAlreadyCorrectFileNameVerifyNoop()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var nuspec = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0.0"
                };

                var zipFile = nuspec.CreateNupkg().Save(workingDir);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "updatefilename", zipFile.FullName }, log);

                // Assert
                Assert.Equal(0, exitCode);
                Assert.True(File.Exists(zipFile.FullName));
            }
        }

        [Fact]
        public async Task GivenThatIUpdateTheNameOfANupkgVerifyItIsCorrect()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var nuspec = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0.0"
                };

                var zipFile = nuspec.CreateNupkg().Save(workingDir);

                var log = new TestLogger();

                var altPath = Path.Combine(zipFile.Directory.FullName, "test.nupkg");
                var origPath = zipFile.FullName;

                File.Move(origPath, altPath);

                // Act
                var exitCode = await Program.MainCore(new[] { "updatefilename", altPath }, log);

                // Assert
                Assert.Equal(0, exitCode);
                Assert.True(File.Exists(origPath), string.Join("|", log.Messages));
            }
        }

        [Fact]
        public async Task GivenThatTheExpectedFileNameAlreadyExistsVerifyItIsReplaced()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var nuspec = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0.0"
                };

                var zipFile = nuspec.CreateNupkg().Save(workingDir);
                var altPath = Path.Combine(zipFile.Directory.FullName, "test.nupkg");
                var expectedPath = zipFile.FullName;

                File.Move(expectedPath, altPath);

                var existingNupkg = nuspec.CreateNupkg();
                existingNupkg.AddFile("old.txt");
                existingNupkg.Save(workingDir).FullName.Should().Be(expectedPath);

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "updatefilename", altPath }, log);

                // Assert
                exitCode.Should().Be(0, log.GetMessages());
                log.GetMessages().Should().Contain($"removing : {expectedPath}");
                File.Exists(altPath).Should().BeFalse();

                using (var reader = new PackageArchiveReader(expectedPath))
                {
                    reader.GetFiles().Should().NotContain("old.txt");
                }
            }
        }
    }
}
