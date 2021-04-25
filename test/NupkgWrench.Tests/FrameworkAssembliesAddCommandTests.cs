using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using FluentAssertions;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Test.Helpers;
using Xunit;

namespace NupkgWrench.Tests
{
    public class FrameworkAssembliesAddCommandTests
    {
        [Fact]
        public void GivenThatIAddAFrameworkAssemblyWithAnExistingEntryVerifyNoFrameworksClears()
        {
            var root = new XElement(XName.Get("frameworkAssemblies"));

            var frameworks1 = new HashSet<NuGetFramework>() { NuGetFramework.Parse("net45") };
            Util.AddFrameworkAssemblyReference(frameworks1, root, "a");

            var frameworks2 = new HashSet<NuGetFramework>();
            Util.AddFrameworkAssemblyReference(frameworks2, root, "a");

            var entry = root.Elements().Single();
            var framework = entry.Attribute(XName.Get("targetFramework"));
            var name = entry.Attribute(XName.Get("assemblyName"));
            name.Value.Should().Be("a");
            framework.Should().BeNull();
        }

        [Fact]
        public void GivenThatIAddAFrameworkAssemblyWithAnExistingEntryVerifyNewItemAdded()
        {
            var root = new XElement(XName.Get("frameworkAssemblies"));

            var frameworks1 = new HashSet<NuGetFramework>() { NuGetFramework.Parse("net45") };
            Util.AddFrameworkAssemblyReference(frameworks1, root, "a");

            var frameworks2 = new HashSet<NuGetFramework>() { NuGetFramework.Parse("net46") };
            Util.AddFrameworkAssemblyReference(frameworks2, root, "b");

            var entry1 = root.Elements().First();
            var framework = entry1.Attribute(XName.Get("targetFramework"));
            var name = entry1.Attribute(XName.Get("assemblyName"));
            name.Value.Should().Be("a");
            framework.Value.Should().Be("net45");

            var entry2 = root.Elements().Skip(1).First();
            var framework2 = entry2.Attribute(XName.Get("targetFramework"));
            var name2 = entry2.Attribute(XName.Get("assemblyName"));
            name2.Value.Should().Be("b");
            framework2.Value.Should().Be("net46");
        }

        [Fact]
        public void GivenThatIAddAFrameworkAssemblyWithAnExistingEntryVerifyFrameworkAppended()
        {
            var root = new XElement(XName.Get("frameworkAssemblies"));

            var frameworks1 = new HashSet<NuGetFramework>() { NuGetFramework.Parse("net45") };
            Util.AddFrameworkAssemblyReference(frameworks1, root, "test");

            var frameworks2 = new HashSet<NuGetFramework>() { NuGetFramework.Parse("net46") };
            Util.AddFrameworkAssemblyReference(frameworks2, root, "test");

            var entry = root.Elements().Single();
            var framework = entry.Attribute(XName.Get("targetFramework"));
            var name = entry.Attribute(XName.Get("assemblyName"));
            name.Value.Should().Be("test");
            framework.Value.Should().Be("net45,net46");
        }

        [Fact]
        public void GivenThatIAddAFrameworkAssemblyWithAnExistingEntryVerifyNoop()
        {
            var root = new XElement(XName.Get("frameworkAssemblies"));

            var frameworks = new HashSet<NuGetFramework>() { NuGetFramework.Parse("net45") };
            Util.AddFrameworkAssemblyReference(frameworks, root, "test");
            Util.AddFrameworkAssemblyReference(frameworks, root, "test");

            var entry = root.Elements().Single();
            var framework = entry.Attribute(XName.Get("targetFramework"));
            var name = entry.Attribute(XName.Get("assemblyName"));

            entry.Name.LocalName.Should().Be("frameworkAssembly");
            name.Value.Should().Be("test");
            framework.Value.Should().Be("net45");
        }

        [Fact]
        public void GivenThatIAddFrameworkAssembliesVerifyTheNuspecXML()
        {
            var assemblies = new HashSet<string>() { "a", "b", "c" };
            var frameworks = new HashSet<NuGetFramework>() { NuGetFramework.Parse("net45"), NuGetFramework.Parse("net46") };

            var doc = new XDocument(new XElement(XName.Get("package"), new XElement(XName.Get("metadata"))));

            Util.AddFrameworkAssemblyReferences(doc, assemblies, frameworks);

            var root = Util.GetMetadataElement(doc).Elements().SingleOrDefault(e => e.Name.LocalName == "frameworkAssemblies");

            root.Elements().Count().Should().Be(3);

            var found = new HashSet<string>();

            foreach (var entry in root.Elements())
            {
                var framework = entry.Attribute(XName.Get("targetFramework"));
                var name = entry.Attribute(XName.Get("assemblyName"));
                found.Add(name.Value);
                entry.Name.LocalName.Should().Be("frameworkAssembly");
                framework.Value.Should().Be("net45,net46");
            }

            assemblies.ShouldBeEquivalentTo(found);
        }

        [Fact]
        public void GivenThatIAddAFrameworkAssemblyVerifyTheNuspecXML()
        {
            var assemblies = new HashSet<string>() { "test" };
            var frameworks = new HashSet<NuGetFramework>() { NuGetFramework.Parse("net45") };

            var doc = new XDocument(new XElement(XName.Get("package"), new XElement(XName.Get("metadata"))));

            Util.AddFrameworkAssemblyReferences(doc, assemblies, frameworks);

            var root = Util.GetMetadataElement(doc).Elements().SingleOrDefault(e => e.Name.LocalName == "frameworkAssemblies");

            var entry = root.Elements().Single();
            var framework = entry.Attribute(XName.Get("targetFramework"));
            var name = entry.Attribute(XName.Get("assemblyName"));

            entry.Name.LocalName.Should().Be("frameworkAssembly");
            name.Value.Should().Be("test");
            framework.Value.Should().Be("net45");
        }

        [Fact]
        public void GivenThatIAddAFrameworkAssemblyVerifyTheNuspecEntry()
        {
            var frameworks = new HashSet<NuGetFramework>() { NuGetFramework.Parse("net45") };

            var root = new XElement(XName.Get("frameworkAssemblies"));

            Util.AddFrameworkAssemblyReference(frameworks, root, "test");

            var entry = root.Elements().Single();
            var framework = entry.Attribute(XName.Get("targetFramework"));
            var name = entry.Attribute(XName.Get("assemblyName"));

            entry.Name.LocalName.Should().Be("frameworkAssembly");
            name.Value.Should().Be("test");
            framework.Value.Should().Be("net45");
        }

        [Fact]
        public void GivenThatIAddAFrameworkAssemblyForMultipleFrameworksVerifyTheNuspecEntry()
        {
            var frameworks = new HashSet<NuGetFramework>() { NuGetFramework.Parse("net45"), NuGetFramework.Parse("net46") };

            var root = new XElement(XName.Get("frameworkAssemblies"));

            Util.AddFrameworkAssemblyReference(frameworks, root, "test");

            var entry = root.Elements().Single();
            var framework = entry.Attribute(XName.Get("targetFramework"));
            var name = entry.Attribute(XName.Get("assemblyName"));

            entry.Name.LocalName.Should().Be("frameworkAssembly");
            name.Value.Should().Be("test");
            framework.Value.Should().Be("net45,net46");
        }

        [Fact]
        public void GivenThatIAddAFrameworkAssemblyWithNoFrameworksVerifyTheNuspecEntry()
        {
            var frameworks = new HashSet<NuGetFramework>();

            var root = new XElement(XName.Get("frameworkAssemblies"));

            Util.AddFrameworkAssemblyReference(frameworks, root, "test");

            var entry = root.Elements().Single();
            var framework = entry.Attribute(XName.Get("targetFramework"));
            var name = entry.Attribute(XName.Get("assemblyName"));

            entry.Name.LocalName.Should().Be("frameworkAssembly");
            name.Value.Should().Be("test");
            framework.Should().BeNull();
        }

        [Fact]
        public async Task GivenThatIAddAFrameworkAssemblyToAnEmptyNuspecVerifyItIsAdded()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var nuspec = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0.0"
                };

                var nupkg = nuspec.CreateNupkg();
                nupkg.Files.Clear();
                nupkg.AddFile("lib/net45/a.dll");
                nupkg.AddFile("lib/netstandard1.0/a.dll");

                var path = nupkg.Save(workingDir).FullName;

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "frameworkassemblies", "add", path, "--assembly-name", "test" }, log);

                NuspecReader reader = null;

                using (var package = new PackageArchiveReader(path))
                {
                    reader = package.NuspecReader;
                }

                // Assert
                exitCode.Should().Be(0, "no errors");

                reader.GetFrameworkReferenceGroups().Count().Should().Be(1, "packages based frameworks are ignored");
                reader.GetFrameworkReferenceGroups().Single().Items.Count().Should().Be(1);
                reader.GetFrameworkReferenceGroups().Single().Items.Single().Should().Be("test");
                reader.GetFrameworkReferenceGroups().Single().TargetFramework.ShouldBeEquivalentTo(NuGetFramework.Parse("net45"));
            }
        }

        [Fact]
        public async Task GivenThatIAddMultipleFrameworkAssembliesToAnEmptyNuspecVerifyItIsAdded()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var nuspec = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0.0"
                };

                var nupkg = nuspec.CreateNupkg();
                nupkg.Files.Clear();
                nupkg.AddFile("lib/net45/a.dll");
                nupkg.AddFile("lib/netstandard1.0/a.dll");

                var path = nupkg.Save(workingDir).FullName;

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "frameworkassemblies", "add", path, "--assembly-name", "testA", "--assembly-name", "testB", "--framework", "net60", "--framework", "net70" }, log);

                NuspecReader reader = null;

                using (var package = new PackageArchiveReader(path))
                {
                    reader = package.NuspecReader;
                }

                // Assert
                exitCode.Should().Be(0, "no errors");

                reader.GetFrameworkReferenceGroups().Count().Should().Be(2);
                reader.GetFrameworkReferenceGroups().Select(e => e.TargetFramework.GetShortFolderName()).ShouldBeEquivalentTo(new[] { "net60", "net70" });

                foreach (var group in reader.GetFrameworkReferenceGroups())
                {
                    group.Items.ShouldBeEquivalentTo(new[] { "testA", "testB" });
                }
            }
        }

        [Fact]
        public async Task GivenThatIAddAFrameworkAssemblyVerifyNoAssemblyNameFails()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var nuspec = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0.0"
                };

                var nupkg = nuspec.CreateNupkg();
                var path = nupkg.Save(workingDir).FullName;

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "frameworkassemblies", "add", path }, log);

                NuspecReader reader = null;
                using (var package = new PackageArchiveReader(path))
                {
                    reader = package.NuspecReader;
                }

                // Assert
                exitCode.Should().Be(1, "error expected");
                string.Join("|", log.Messages.Where(e => e.Level == LogLevel.Error).Select(e => e.Message)).Should().Contain("--assembly-name");
            }
        }

        [Fact]
        public async Task GivenThatIAddAFrameworkAssemblyVerifyPackagesBasedFrameworkFails()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var nuspec = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0.0"
                };

                var nupkg = nuspec.CreateNupkg();
                var path = nupkg.Save(workingDir).FullName;

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "frameworkassemblies", "add", path, "--assembly-name", "test", "--framework", "netstandard1.0" }, log);

                NuspecReader reader = null;
                using (var package = new PackageArchiveReader(path))
                {
                    reader = package.NuspecReader;
                }

                // Assert
                exitCode.Should().Be(1, "error expected");
                string.Join("|", log.Messages.Where(e => e.Level == LogLevel.Error).Select(e => e.Message)).Should().Contain("Framework assemblies are not supported on packages based frameworks: netstandard1.0");
            }
        }

        [Fact]
        public async Task GivenThatIAddAFrameworkAssemblyVerifyErrorIfNoFrameworkAndFrameworksAreGiven()
        {
            using (var workingDir = new TestFolder())
            {
                // Arrange
                var nuspec = new TestNuspec()
                {
                    Id = "a",
                    Version = "1.0.0"
                };

                var nupkg = nuspec.CreateNupkg();
                var path = nupkg.Save(workingDir).FullName;

                var log = new TestLogger();

                // Act
                var exitCode = await Program.MainCore(new[] { "nuspec", "frameworkassemblies", "add", path, "--assembly-name", "test", "--framework", "net46", "--no-frameworks" }, log);

                NuspecReader reader = null;
                using (var package = new PackageArchiveReader(path))
                {
                    reader = package.NuspecReader;
                }

                // Assert
                exitCode.Should().Be(1, "error expected");
                string.Join("|", log.Messages.Where(e => e.Level == LogLevel.Error).Select(e => e.Message)).Should().Contain("--framework, --no-frameworks may not be used together.");
            }
        }
    }
}
