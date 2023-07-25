// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Profiler.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class ProfilerInitializationTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public ProfilerInitializationTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitectureProfilerPath), MemberType = typeof(ProfilerHelper))]
        public async Task LoadAtStart(Architecture architecture, string profilerPath)
        {
            await using AppRunner runner = new(_outputHelper, Assembly.GetExecutingAssembly());
            runner.Architecture = architecture;
            runner.ScenarioName = TestAppScenarios.AsyncWait.Name;

            // Environment variables necessary for running the profiler + enable all logging to stderr
            string runtimeInstanceId = Guid.NewGuid().ToString("D");
            runner.Environment.Add(ProfilerHelper.ClrEnvVarEnableNotificationProfilers, ProfilerHelper.ClrEnvVarEnabledValue);
            runner.Environment.Add(ProfilerHelper.ClrEnvVarEnableProfiling, ProfilerHelper.ClrEnvVarEnabledValue);
            runner.Environment.Add(ProfilerHelper.ClrEnvVarProfiler, ProfilerIdentifiers.NotifyOnlyProfiler.Clsid.StringWithBraces);
            runner.Environment.Add(ProfilerHelper.ClrEnvVarProfilerPath, profilerPath);
            runner.Environment.Add(ProfilerIdentifiers.EnvironmentVariables.RuntimeInstanceId, runtimeInstanceId);
            runner.Environment.Add(ProfilerIdentifiers.EnvironmentVariables.StdErrLogger_Level, LogLevel.Trace.ToString("G"));

            await runner.ExecuteAsync(async () =>
            {
                // At this point, the profiler has already been initialized and managed code is already running.
                // Use any of the initialization state of the profiler to validate that it is loaded.
                await ProfilerHelper.VerifyProductVersionEnvironmentVariableAsync(runner, _outputHelper);

                VerifySocketPath(Path.GetTempPath(), runtimeInstanceId);

                await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
            });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitectureProfilerPath), MemberType = typeof(ProfilerHelper))]
        public async Task AttachAfterStarted(Architecture architecture, string profilerPath)
        {
            await using AppRunner runner = new(_outputHelper, Assembly.GetExecutingAssembly());
            runner.Architecture = architecture;
            runner.ScenarioName = TestAppScenarios.AsyncWait.Name;

            string runtimeInstanceId = Guid.NewGuid().ToString("D");
            await runner.ExecuteAsync(async () =>
            {
                DiagnosticsClient client = new(await runner.ProcessIdTask);

                client.SetEnvironmentVariable(
                    ProfilerIdentifiers.EnvironmentVariables.RuntimeInstanceId,
                    runtimeInstanceId);

                client.SetEnvironmentVariable(
                    ProfilerIdentifiers.EnvironmentVariables.StdErrLogger_Level,
                    LogLevel.Trace.ToString("G"));

                // Profiler will attach and initialize before this returns.
                // All settings must be applied before issuing attach profiler call.
                client.AttachProfiler(
                    TimeSpan.FromSeconds(10),
                    ProfilerIdentifiers.NotifyOnlyProfiler.Clsid.Guid,
                    profilerPath);

                await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);

                // At this point, the profiler has already been initialized and managed code is already running.
                // Use any of the initialization state of the profiler to validate that it is loaded.
                await ProfilerHelper.VerifyProductVersionEnvironmentVariableAsync(runner, _outputHelper);

                VerifySocketPath(Path.GetTempPath(), runtimeInstanceId);
            });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitectureProfilerPath), MemberType = typeof(ProfilerHelper))]
        public async Task VerifyCustomSharedPath(Architecture architecture, string profilerPath)
        {
            using TemporaryDirectory tempDir = new(_outputHelper);

            await using AppRunner runner = new(_outputHelper, Assembly.GetExecutingAssembly());
            runner.Architecture = architecture;
            runner.ScenarioName = TestAppScenarios.AsyncWait.Name;

            // Environment variables necessary for running the profiler + enable all logging to stderr
            string runtimeInstanceId = Guid.NewGuid().ToString("D");
            runner.Environment.Add(ProfilerHelper.ClrEnvVarEnableNotificationProfilers, ProfilerHelper.ClrEnvVarEnabledValue);
            runner.Environment.Add(ProfilerHelper.ClrEnvVarEnableProfiling, ProfilerHelper.ClrEnvVarEnabledValue);
            runner.Environment.Add(ProfilerHelper.ClrEnvVarProfiler, ProfilerIdentifiers.NotifyOnlyProfiler.Clsid.StringWithBraces);
            runner.Environment.Add(ProfilerHelper.ClrEnvVarProfilerPath, profilerPath);
            runner.Environment.Add(ProfilerIdentifiers.EnvironmentVariables.RuntimeInstanceId, runtimeInstanceId);
            runner.Environment.Add(ProfilerIdentifiers.EnvironmentVariables.SharedPath, tempDir.FullName);
            runner.Environment.Add(ProfilerIdentifiers.EnvironmentVariables.StdErrLogger_Level, LogLevel.Trace.ToString("G"));

            await runner.ExecuteAsync(async () =>
            {
                // At this point, the profiler has already been initialized and managed code is already running.
                VerifySocketPath(tempDir.FullName, runtimeInstanceId);

                await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
            });
        }

        private static void VerifySocketPath(string directoryPath, string runtimeInstanceId)
        {
            string expectedPath = Path.Combine(directoryPath, $"{runtimeInstanceId}.sock");
            Assert.True(File.Exists(expectedPath), $"Expected socket file at '{expectedPath}'.");
        }
    }
}
