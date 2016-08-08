using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace NupkgWrench.Tests
{
    public static class TestUtility
    {
        public static Stream GetResource(string name)
        {
            var path = $"NupkgWrench.Tests.compiler.resources.{name}";
            return typeof(TestUtility).GetTypeInfo().Assembly.GetManifestResourceStream(path);
        }

        public static XDocument GetXml(string name)
        {
            return XDocument.Load(GetResource(name));
        }
    }
}
