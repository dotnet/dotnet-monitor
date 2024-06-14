// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Tools.Monitor.StartupHook
{
    public static class InProcessFeaturesIdentifiers
    {
        public static class EnvironmentVariables
        {
            private const string InProcessFeaturesPrefix = ToolIdentifiers.StandardPrefix + "InProcessFeatures_";

            public static class Exceptions
            {
                private const string Prefix = InProcessFeaturesPrefix + "Exceptions_";
                public const string IncludeMonitorExceptions = Prefix + nameof(IncludeMonitorExceptions);
            }

            public static class ParameterCapturing
            {
                private const string Prefix = InProcessFeaturesPrefix + "ParameterCapturing_";
                public const string Enable = Prefix + nameof(Enable);
            }

            public static class AvailableInfrastructure
            {
                private const string AvailableInfrastructurePrefix = InProcessFeaturesPrefix + "AvailableInfrastructure_";
                public const string ManagedMessaging = AvailableInfrastructurePrefix + nameof(ManagedMessaging);
                public const string StartupHook = AvailableInfrastructurePrefix + nameof(StartupHook);
            }
        }
    }
}
