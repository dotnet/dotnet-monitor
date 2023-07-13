// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.TestCommon
#else
namespace Microsoft.Diagnostics.Tools.Monitor
#endif
{
    internal static class ToolIdentifiers
    {
        /// <summary>
        /// The standard prefix for environment variables and dotnet-monitor specific configuration.
        /// </summary>
        public const string StandardPrefix = "DotnetMonitor_";

        public const string DefaultSocketName = "dotnet-monitor.sock";

        public static class EnvironmentVariables
        {
            // This environment variable is manually applied to target processes to inform dotnet-monitor
            // which runtime variant of the shared libraries should be loaded into target processes.
            public const string RuntimeIdentifier = StandardPrefix + nameof(RuntimeIdentifier);

            public const string StartupHooks = "DOTNET_STARTUP_HOOKS";
        }
    }
}
