// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class ConfigurationKeys
    {
        public const string Authentication = nameof(RootOptions.Authentication);

        public const string AzureAd = nameof(AuthenticationOptions.AzureAd);

        public const string CollectionRules = nameof(CollectionRules);

        public const string MonitorApiKey = nameof(AuthenticationOptions.MonitorApiKey);

        public const string CorsConfiguration = nameof(RootOptions.CorsConfiguration);

        public const string DiagnosticPort = nameof(RootOptions.DiagnosticPort);

        public const string InProcessFeatures = nameof(RootOptions.InProcessFeatures);

        public const string Egress = nameof(RootOptions.Egress);

        public const string Metrics = nameof(RootOptions.Metrics);

        public const string Storage = nameof(RootOptions.Storage);

        public const string DefaultProcess = nameof(DefaultProcess);

        public const string Logging = nameof(Logging);

        public const string GlobalCounter = nameof(RootOptions.GlobalCounter);

        public const string CollectionRuleDefaults = nameof(RootOptions.CollectionRuleDefaults);

        public const string Templates = nameof(RootOptions.Templates);
    }
}
