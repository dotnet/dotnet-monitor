// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

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
    }
}
