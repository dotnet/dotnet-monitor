// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.TestCommon
#else
namespace Microsoft.Diagnostics.Tools.Monitor.StartupHook
#endif
{
    public static class StartupHookIdentifiers
    {
        public static class EnvironmentVariables
        {
            private const string StartupHookPrefix = ToolIdentifiers.StandardPrefix + "StartupHook_";

            // This environment variable is automatically applied to a target process by the tool to inform
            // the profiler which directory it should use to share files and information with dotnet-monitor.
            public const string SharedPath = StartupHookPrefix + nameof(SharedPath);

            // This environment variable name is embedded into the profiler and set at profiler initialization.
            // The value is determined BEFORE native build by the generation of the product version into the
            // _productversion.h header file.
            public const string HostingStartupPath = StartupHookPrefix + nameof(HostingStartupPath);


        }
    }
}
