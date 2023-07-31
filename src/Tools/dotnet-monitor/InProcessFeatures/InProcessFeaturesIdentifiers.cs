// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Tools.Monitor.HostingStartup
{
    public static class InProcessFeaturesIdentifiers
    {
        public static class EnvironmentVariables
        {
            private const string InProcessFeaturesPrefix = ToolIdentifiers.StandardPrefix + "InProcessFeatures_";

            public const string EnableParameterCapturing = InProcessFeaturesPrefix + nameof(EnableParameterCapturing);


            public static class AvailableInfrastructure
            {
                private const string AvailableInfrastructurePrefix = InProcessFeaturesPrefix + "AvailableInfrastructure_";
                public const string ManagedMessaging = AvailableInfrastructurePrefix + nameof(ManagedMessaging);
                public const string StartupHook = AvailableInfrastructurePrefix + nameof(StartupHook);
                public const string HostingStartup = AvailableInfrastructurePrefix + nameof(HostingStartup);
            }
        }
    }
}
