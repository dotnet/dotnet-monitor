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
    internal static class ProfilerHelper
    {
        private const string ClrEnvVarPrefix = "CORECLR_";

        public const string ClrEnvVarEnabledValue = "1";
        public const string ClrEnvVarEnableNotificationProfilers = ClrEnvVarPrefix + "ENABLE_NOTIFICATION_PROFILERS";
        public const string ClrEnvVarEnableProfiling = ClrEnvVarPrefix + "ENABLE_PROFILING";
        public const string ClrEnvVarProfiler = ClrEnvVarPrefix + "PROFILER";
        public const string ClrEnvVarProfilerPath64 = ClrEnvVarPrefix + "PROFILER_PATH_64";

        private const string ProfilerEnvVarPrefix = "DotnetMonitorProfiler_";

        // This environment variable name is embedded into the profiler and set at profiler initialization.
        // The value is determined BEFORE native build by the generation of the product version into the
        // _productversion.h header file.
        public const string ProfilerEnvVarProductVersion = ProfilerEnvVarPrefix + "ProductVersion";
        public const string ProfilerEnvVarRuntimeId = ProfilerEnvVarPrefix + "RuntimeId";
        public const string ProfilerEnvVarStdErrLoggerLevel = ProfilerEnvVarPrefix + "StdErrLogger_Level";

        public static readonly Guid Clsid = new Guid("6A494330-5848-4A23-9D87-0E57BBF6DE79");

        public static string GetPath(Architecture architecture) =>
            NativeLibraryHelper.GetSharedLibraryPath(architecture, "MonitorProfiler");

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
            string productVersion = await runner.GetEnvironmentVariable(ProfilerEnvVarProductVersion, CommonTestTimeouts.EnvVarsTimeout);
            Assert.False(string.IsNullOrEmpty(productVersion), "Expected product version to not be null or empty.");
            outputHelper.WriteLine("{0} = {1}", ProfilerEnvVarProductVersion, productVersion);
        }
    }
}
