using System.Xml.Linq;
using AwesomeAssertions;
using NuGet.Frameworks;
using Xunit;

namespace NupkgWrench.Tests
{
    public class DependenciesUtilTests
    {
        private const string NuspecNamespace = "http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd";

        [Theory]
        [InlineData("net45")]
        [InlineData("net451")]
        [InlineData("net472")]
        [InlineData("netcoreapp3.1")]
        [InlineData("netstandard2.0")]
        [InlineData("net5.0")]
        [InlineData("net8.0")]
        [InlineData("net10.0")]
        [InlineData("uap10.0.16299")]
        [InlineData("net6.0-windows")]
        [InlineData("net8.0-android34.0")]
        [InlineData("net8.0-windows10.0.19041")]
        [InlineData("net40-client")]
        [InlineData("portable-net45+win8")]
        public void GivenAFrameworkVerifyTheGroupTargetFrameworkRoundTrips(string framework)
        {
            // Arrange
            var expected = NuGetFramework.Parse(framework);

            // Act
            var group = DependenciesUtil.CreateGroupNode(NuspecNamespace, expected);

            // Assert
            group.Name.Should().Be(XName.Get("group", NuspecNamespace));
            NuGetFramework.Parse(group.Attribute("targetFramework").Value).Should().Be(expected);
        }

        [Fact]
        public void GivenTheAnyFrameworkVerifyTheGroupHasNoTargetFramework()
        {
            // Act
            var group = DependenciesUtil.CreateGroupNode(NuspecNamespace, NuGetFramework.AnyFramework);

            // Assert
            group.Name.Should().Be(XName.Get("group", NuspecNamespace));
            group.Attributes().Should().BeEmpty();
        }
    }
}
