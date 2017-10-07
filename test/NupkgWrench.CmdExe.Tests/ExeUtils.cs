using System;
using System.Collections.Generic;
using System.Text;
using Test.Common;

namespace NupkgWrench.CmdExe.Tests
{
    public static class ExeUtils
    {
        private static readonly Lazy<string> _getExe = new Lazy<string>(() => CmdRunner.GetPath("artifacts/publish/NupkgWrench.exe"));

        public static string NupkgWrenchExePath => _getExe.Value;
    }
}
