// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class ProfilerHelper
    {
        private const string ClrEnvVarPrefix = "CORECLR_";

        public const string ClrEnvVarEnabledValue = "1";
        public const string ClrEnvVarEnableNotificationProfilers = ClrEnvVarPrefix + "ENABLE_NOTIFICATION_PROFILERS";
        public const string ClrEnvVarEnableProfiling = ClrEnvVarPrefix + "ENABLE_PROFILING";
        public const string ClrEnvVarProfiler = ClrEnvVarPrefix + "PROFILER";
        public const string ClrEnvVarProfilerPath = ClrEnvVarPrefix + "PROFILER_PATH";

        public static string GetNotifyOnlyPath(Architecture architecture) =>
            NativeLibraryHelper.GetSharedLibraryPath(architecture, ProfilerIdentifiers.NotifyOnlyProfiler.LibraryRootFileName);

        public static IEnumerable<object[]> GetArchitecture()
        {
            // There isn't a good way to check which architecture to use when running unit tests.
            // Each build job builds one specific architecture, but from a test perspective,
            // it cannot tell which one was built. Gather all of the profilers for every architecture
            // so long as they exist.
            List<object[]> arguments = new();
            AddTestCases(arguments, Architecture.X64);
            AddTestCases(arguments, Architecture.X86);
            AddTestCases(arguments, Architecture.Arm64);
            return arguments;

            static void AddTestCases(List<object[]> arguments, Architecture architecture)
            {
                string profilerPath = GetNotifyOnlyPath(architecture);
                if (File.Exists(profilerPath))
                {
                    arguments.Add(new object[] { architecture });
                }
            }
        }

        public static IEnumerable<object[]> GetArchitectureProfilerPath()
        {
            // There isn't a good way to check which architecture to use when running unit tests.
            // Each build job builds one specific architecture, but from a test perspective,
            // it cannot tell which one was built. Gather all of the profilers for every architecture
            // so long as they exist.
            List<object[]> arguments = new();
            AddTestCases(arguments, Architecture.X64);
            AddTestCases(arguments, Architecture.X86);
            AddTestCases(arguments, Architecture.Arm64);
            return arguments;

            static void AddTestCases(List<object[]> arguments, Architecture architecture)
            {
                string profilerPath = GetNotifyOnlyPath(architecture);
                if (File.Exists(profilerPath))
                {
                    arguments.Add(new object[] { architecture, profilerPath });
                }
            }
        }

        public static async Task VerifyProductVersionEnvironmentVariableAsync(AppRunner runner, ITestOutputHelper outputHelper)
        {
            string productVersion = await runner.GetEnvironmentVariable(ProfilerIdentifiers.NotifyOnlyProfiler.EnvironmentVariables.ProductVersion, CommonTestTimeouts.EnvVarsTimeout);
            Assert.False(string.IsNullOrEmpty(productVersion), "Expected product version to not be null or empty.");
            outputHelper.WriteLine("{0} = {1}", ProfilerIdentifiers.NotifyOnlyProfiler.EnvironmentVariables.ProductVersion, productVersion);
        }

        public static async Task WaitForProfilerCommunicationChannelAsync(ProcessInfo processInfo)
        {
            string channelPath = Path.Combine(Path.GetTempPath(), FormattableString.Invariant($"{processInfo.Uid:D}.sock"));

            using CancellationTokenSource cancellationSource = new CancellationTokenSource(CommonTestTimeouts.GeneralTimeout);

            while (!File.Exists(channelPath))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationSource.Token);
            }
        }
    }
}
