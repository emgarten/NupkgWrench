using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NuGet.Test.Helpers;
using Test.Common;
using Xunit;

namespace NupkgWrench.CmdExe.Tests
{
    public class BasicTests
    {
        [WindowsTheory]
        [InlineData("foo")]
        [InlineData("list --foo")]
        [InlineData("id")]
        public async Task GivenABadCommandVerifyFailure(string arguments)
        {
            using (var workingDir = new TestFolder())
            {
                var result = await CmdRunner.RunAsync(ExeUtils.NupkgWrenchExePath, workingDir, arguments);

                result.Success.Should().BeFalse();
            }
        }

        [WindowsFact]
        public async Task RunVersionCommandVerifySuccess()
        {
            using (var workingDir = new TestFolder())
            {
                var args = "--version";

                var result = await CmdRunner.RunAsync(ExeUtils.NupkgWrenchExePath, workingDir, args);

                result.Success.Should().BeTrue();
                result.Errors.Should().BeNullOrEmpty();
            }
        }

        [WindowsFact]
        public async Task RunModifyNuspecVerifySuccess()
        {
            using (var workingDir = new TestFolder())
            {
                var testNupkg = new TestNupkg("a", "1.0.0");
                TestNupkg.Create(workingDir.Root);

                var args = "nuspec dependencies clear";

                var result = await CmdRunner.RunAsync(ExeUtils.NupkgWrenchExePath, workingDir, args);

                result.Success.Should().BeTrue();
                result.Errors.Should().BeNullOrEmpty();
            }
        }
    }
}
