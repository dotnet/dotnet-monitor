// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class ConfigurationKeys
    {
        public const string Authentication = nameof(RootOptions.Authentication);

        public const string AzureAd = nameof(AzureAd);

        public const string CollectionRules = nameof(CollectionRules);

        public const string MonitorApiKey = nameof(AuthenticationOptions.MonitorApiKey);

        public const string CorsConfiguration = nameof(RootOptions.CorsConfiguration);

        public const string DiagnosticPort = nameof(RootOptions.DiagnosticPort);

        public const string InProcessFeatures = nameof(RootOptions.InProcessFeatures);

        public const string DotnetMonitorDebug = nameof(RootOptions.DotnetMonitorDebug);

        public static readonly string InProcessFeatures_CallStacks = ConfigurationPath.Combine(InProcessFeatures, "CallStacks");

        public static readonly string InProcessFeatures_Enabled = ConfigurationPath.Combine(InProcessFeatures, "Enabled");

        public static readonly string InProcessFeatures_Exceptions = ConfigurationPath.Combine(InProcessFeatures, "Exceptions");

        public static readonly string InProcessFeatures_ParameterCapturing = ConfigurationPath.Combine(InProcessFeatures, "ParameterCapturing");

        public static readonly string DotnetMonitorDebug_Exceptions = ConfigurationPath.Combine(DotnetMonitorDebug, "Exceptions");

        public const string Egress = nameof(Egress);

        public const string Egress_Properties = "Properties";

        public const string Metrics = nameof(RootOptions.Metrics);

        public const string Storage = nameof(RootOptions.Storage);

        public const string DefaultProcess = nameof(RootOptions.DefaultProcess);

        public const string Logging = nameof(Logging);

        public const string GlobalCounter = nameof(RootOptions.GlobalCounter);

        public const string CollectionRuleDefaults = nameof(RootOptions.CollectionRuleDefaults);

        public const string Templates = nameof(RootOptions.Templates);

        public const string InternalHostBuilderSettings = nameof(InternalHostBuilderSettings);
    }
}
