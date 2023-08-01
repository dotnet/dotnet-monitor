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
    public enum ProfilerVariant
    {
        NotifyOnly,
        Mutating
    }

    public static class ProfilerHelper
    {
        private const string ClrEnvVarPrefix = "CORECLR_";

        public const string ClrEnvVarEnabledValue = "1";
        public const string ClrEnvVarEnableNotificationProfilers = ClrEnvVarPrefix + "ENABLE_NOTIFICATION_PROFILERS";
        public const string ClrEnvVarEnableProfiling = ClrEnvVarPrefix + "ENABLE_PROFILING";
        public const string ClrEnvVarProfiler = ClrEnvVarPrefix + "PROFILER";
        public const string ClrEnvVarProfilerPath = ClrEnvVarPrefix + "PROFILER_PATH";

        public static string GetPath(Architecture architecture, ProfilerVariant variant = ProfilerVariant.NotifyOnly) =>
            NativeLibraryHelper.GetSharedLibraryPath(architecture,
                variant == ProfilerVariant.NotifyOnly
                ? ProfilerIdentifiers.NotifyOnlyProfiler.LibraryRootFileName
                : ProfilerIdentifiers.MutatingProfiler.LibraryRootFileName);

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
                // Both profiler variants support the same architecture, so simply use the notify-only one to check
                // which are available.
                string profilerPath = GetPath(architecture, ProfilerVariant.NotifyOnly);
                if (File.Exists(profilerPath))
                {
                    arguments.Add(new object[] { architecture });
                }
            }
        }

        public static IEnumerable<object[]> GetNotifyOnlyArchitectureProfilerPath()
        {
            return GetArchitectureProfilerPathCore(ProfilerVariant.NotifyOnly);
        }

        public static IEnumerable<object[]> GetMutatingArchitectureProfilerPath()
        {
            return GetArchitectureProfilerPathCore(ProfilerVariant.Mutating);
        }

        public static IEnumerable<object[]> GetArchitectureProfilerPathCore(ProfilerVariant variant)
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

            void AddTestCases(List<object[]> arguments, Architecture architecture)
            {
                string profilerPath = GetPath(architecture, variant);
                if (File.Exists(profilerPath))
                {
                    arguments.Add(new object[] { architecture, profilerPath, variant });
                }
            }
        }

        public static async Task VerifyProductVersionEnvironmentVariableAsync(AppRunner runner, ITestOutputHelper outputHelper, ProfilerVariant variant = ProfilerVariant.NotifyOnly)
        {
            string envVar =
                variant == ProfilerVariant.NotifyOnly
                ? ProfilerIdentifiers.NotifyOnlyProfiler.EnvironmentVariables.ProductVersion
                : ProfilerIdentifiers.MutatingProfiler.EnvironmentVariables.ProductVersion;

            string productVersion = await runner.GetEnvironmentVariable(envVar, CommonTestTimeouts.EnvVarsTimeout);
            Assert.False(string.IsNullOrEmpty(productVersion), "Expected product version to not be null or empty.");
            outputHelper.WriteLine("{0} = {1}", envVar, productVersion);
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
