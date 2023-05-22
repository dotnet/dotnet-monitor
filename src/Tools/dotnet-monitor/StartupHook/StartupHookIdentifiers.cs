// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Tools.Monitor.StartupHook
{
    public static class StartupHookIdentifiers
    {
        public static class EnvironmentVariables
        {
            private const string StartupHookPrefix = ToolIdentifiers.StandardPrefix + "StartupHook_";

            // The full path of a HostingStartup assembly to load into a target process.
            public const string HostingStartupPath = StartupHookPrefix + nameof(HostingStartupPath);
        }
    }
}
