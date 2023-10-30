// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class DiagnosticPortTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public DiagnosticPortTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// When setting the default shared path in connect mode, a server socket should not be created.
        /// </summary>
        [Fact]
        public async Task DefaultDiagnosticPort_NotSupported_ConnectMode()
        {
            using TemporaryDirectory defaultSharedTempDir = new(_outputHelper);

            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.ConnectionModeViaCommandLine = WebApi.DiagnosticPortConnectionMode.Connect;
            toolRunner.ConfigurationFromEnvironment.SetDefaultSharedPath(defaultSharedTempDir.FullName);

            await toolRunner.StartAsync();

            AssertDefaultDiagnosticPortNotExists(defaultSharedTempDir);
        }

        /// <summary>
        /// When setting the default shared path in listen mode, a server socket should not be created
        /// under the default shared path if the endpoint for the diagnostic port is specified.
        /// </summary>
        [Fact]
        public async Task DefaultDiagnosticPort_NotSupported_ListenModeWithSpecifiedPort()
        {
            using TemporaryDirectory defaultSharedTempDir = new(_outputHelper);

            DiagnosticPortHelper.Generate(
                DiagnosticPortConnectionMode.Listen,
                out _,
                out string diagnosticPortPath);

            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.ConnectionModeViaCommandLine = DiagnosticPortConnectionMode.Listen;
            toolRunner.DiagnosticPortPath = diagnosticPortPath;
            toolRunner.ConfigurationFromEnvironment.SetDefaultSharedPath(defaultSharedTempDir.FullName);

            await toolRunner.StartAsync();

            AssertDefaultDiagnosticPortNotExists(defaultSharedTempDir);
        }

        /// <summary>
        /// When setting the default shared path in listen mode on non-Windows platform,
        /// a server socket should be created under the default shared path.
        /// </summary>
        [ConditionalFact(typeof(TestConditions), nameof(TestConditions.IsNotWindows))]
        public async Task DefaultDiagnosticPort_Supported_ListenModeOnNonWindows()
        {
            using TemporaryDirectory defaultSharedTempDir = new(_outputHelper);

            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.ConfigurationFromEnvironment.SetConnectionMode(DiagnosticPortConnectionMode.Listen);
            toolRunner.ConfigurationFromEnvironment.SetDefaultSharedPath(defaultSharedTempDir.FullName);

            await toolRunner.StartAsync();

            AssertDefaultDiagnosticPortExists(defaultSharedTempDir);
        }

        /// <summary>
        /// When setting the default shared path in listen mode on Windows platform,
        /// a server socket should not be created under the default shared path.
        /// </summary>
        [ConditionalFact(typeof(TestConditions), nameof(TestConditions.IsWindows))]
        public async Task DefaultDiagnosticPort_NotSupported_ListenModeOnWindows()
        {
            using TemporaryDirectory defaultSharedTempDir = new(_outputHelper);

            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.ConfigurationFromEnvironment.SetConnectionMode(DiagnosticPortConnectionMode.Listen);
            toolRunner.ConfigurationFromEnvironment.SetDefaultSharedPath(defaultSharedTempDir.FullName);

            // dotnet-monitor will fail to start due to misconfigured diagnostic port
            await Assert.ThrowsAsync<InvalidOperationException>(toolRunner.StartAsync);

            AssertDefaultDiagnosticPortNotExists(defaultSharedTempDir);
        }

        private static string GetDefaultSharedSocketPath(string defaultSharedPath)
        {
            return Path.Combine(defaultSharedPath, ToolIdentifiers.DefaultSocketName);
        }

        private static void AssertDefaultDiagnosticPortExists(TemporaryDirectory dir)
        {
            string diagnosticPort = GetDefaultSharedSocketPath(dir.FullName);
            Assert.True(File.Exists(diagnosticPort), $"Expected socket to exist at '{diagnosticPort}'.");
        }

        private static void AssertDefaultDiagnosticPortNotExists(TemporaryDirectory dir)
        {
            string diagnosticPort = GetDefaultSharedSocketPath(dir.FullName);
            Assert.False(File.Exists(diagnosticPort), $"Expected socket to not exist at '{diagnosticPort}'.");
        }
    }
}
