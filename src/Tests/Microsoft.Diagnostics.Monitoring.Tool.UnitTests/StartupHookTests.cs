// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.StartupHook;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class StartupHookTests
    {
        private ITestOutputHelper _outputHelper;

        public StartupHookTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        // It appears that the profiler isn't loading on musl libc distros for this tests.
        [ConditionalTheory(typeof(TestConditions), nameof(TestConditions.IsNotAlpine))]
        [MemberData(nameof(ProfilerHelper.GetNotifyOnlyArchitectureProfilerPath), MemberType = typeof(ProfilerHelper))]
        public async Task StartupHook_WithProfiler_HasManagedMessaging(Architecture architecture, string profilerPath, ProfilerVariant variant)
        {
            Assert.Equal(ProfilerVariant.NotifyOnly, variant);

            await using AppRunner runner = new(
                _outputHelper,
                Assembly.GetExecutingAssembly());
            runner.Architecture = architecture;
            runner.ScenarioName = TestAppScenarios.AsyncWait.Name;
            runner.EnableMonitorStartupHook = true;

            // Environment variables necessary for running the profiler + enable all logging to stderr
            runner.Environment.Add(ProfilerHelper.ClrEnvVarEnableNotificationProfilers, ProfilerHelper.ClrEnvVarEnabledValue);
            runner.Environment.Add(ProfilerHelper.ClrEnvVarEnableProfiling, ProfilerHelper.ClrEnvVarEnabledValue);
            runner.Environment.Add(ProfilerHelper.ClrEnvVarProfiler, ProfilerIdentifiers.NotifyOnlyProfiler.Clsid.StringWithBraces);
            runner.Environment.Add(ProfilerHelper.ClrEnvVarProfilerPath, profilerPath);
            runner.Environment.Add(ProfilerIdentifiers.EnvironmentVariables.RuntimeInstanceId, Guid.NewGuid().ToString("D"));
            runner.Environment.Add(ProfilerIdentifiers.EnvironmentVariables.StdErrLogger_Level, LogLevel.Trace.ToString("G"));

            await runner.ExecuteAsync(async () =>
            {
                DiagnosticsClient client = new(await runner.ProcessIdTask);

                Dictionary<string, string> env = client.GetProcessEnvironment();
                Assert.True(env.TryGetValue(InProcessFeaturesIdentifiers.EnvironmentVariables.AvailableInfrastructure.StartupHook, out string startupHookAvailableValue));
                Assert.True(ToolIdentifiers.IsEnvVarValueEnabled(startupHookAvailableValue));
                Assert.True(env.TryGetValue(InProcessFeaturesIdentifiers.EnvironmentVariables.AvailableInfrastructure.ManagedMessaging, out string managedMessagingAvailableValue));
                Assert.True(ToolIdentifiers.IsEnvVarValueEnabled(startupHookAvailableValue));

                await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
            });
        }

        [Fact]
        public async Task StartupHook_WithoutProfiler_NoManagedMessaging()
        {
            await using AppRunner runner = new(
                _outputHelper,
                Assembly.GetExecutingAssembly());
            runner.ScenarioName = TestAppScenarios.AsyncWait.Name;
            runner.EnableMonitorStartupHook = true;

            await runner.ExecuteAsync(async () =>
            {
                DiagnosticsClient client = new(await runner.ProcessIdTask);

                Dictionary<string, string> env = client.GetProcessEnvironment();
                Assert.True(env.TryGetValue(InProcessFeaturesIdentifiers.EnvironmentVariables.AvailableInfrastructure.StartupHook, out string startupHookAvailableValue));
                Assert.True(ToolIdentifiers.IsEnvVarValueEnabled(startupHookAvailableValue));
                Assert.False(env.TryGetValue(InProcessFeaturesIdentifiers.EnvironmentVariables.AvailableInfrastructure.ManagedMessaging, out _));

                await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
            });
        }
    }
}
