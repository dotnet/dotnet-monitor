// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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

        public static string GetPath(Architecture architecture) =>
            NativeLibraryHelper.GetSharedLibraryPath(architecture, ProfilerIdentifiers.LibraryRootFileName);

        private const string OSReleasePath = "/etc/os-release";

        public static string GetTargetRuntimeIdentifier(Architecture? architecture)
        {
            string architectureString = (architecture ?? RuntimeInformation.OSArchitecture)
                .ToString("G")
                .ToLowerInvariant();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return FormattableString.Invariant($"win-{architectureString}");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return FormattableString.Invariant($"osx-{architectureString}");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (File.Exists(OSReleasePath) && File.ReadAllText(OSReleasePath).Contains("Alpine", StringComparison.OrdinalIgnoreCase))
                {
                    return FormattableString.Invariant($"linux-musl-{architectureString}");
                }
                else
                {
                    return FormattableString.Invariant($"linux-{architectureString}");
                }
            }

            throw new PlatformNotSupportedException("Unable to determine OS platform.");
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
                string profilerPath = GetPath(architecture);
                if (File.Exists(profilerPath))
                {
                    arguments.Add(new object[] { architecture, profilerPath });
                }
            }
        }

        public static async Task VerifyProductVersionEnvironmentVariableAsync(AppRunner runner, ITestOutputHelper outputHelper)
        {
            string productVersion = await runner.GetEnvironmentVariable(ProfilerIdentifiers.EnvironmentVariables.ProductVersion, CommonTestTimeouts.EnvVarsTimeout);
            Assert.False(string.IsNullOrEmpty(productVersion), "Expected product version to not be null or empty.");
            outputHelper.WriteLine("{0} = {1}", ProfilerIdentifiers.EnvironmentVariables.ProductVersion, productVersion);
        }
    }
}
