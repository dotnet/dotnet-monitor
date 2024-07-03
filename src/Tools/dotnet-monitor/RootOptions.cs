// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed partial class RootOptions
    {
        public AuthenticationOptions? Authentication { get; set; }

        public IDictionary<string, CollectionRuleOptions>? CollectionRules { get; set; }
            = new Dictionary<string, CollectionRuleOptions>(0);

        public GlobalCounterOptions? GlobalCounter { get; set; }

        public InProcessFeaturesOptions? InProcessFeatures { get; set; }

        public CorsConfigurationOptions? CorsConfiguration { get; set; }

        public DiagnosticPortOptions? DiagnosticPort { get; set; }

        public EgressOptions? Egress { get; set; }

        public MetricsOptions? Metrics { get; set; }

        public StorageOptions? Storage { get; set; }

        public ProcessFilterOptions? DefaultProcess { get; set; }

        public CollectionRuleDefaultsOptions? CollectionRuleDefaults { get; set; }

        public TemplateOptions? Templates { get; set; }

        public DotnetMonitorDebugOptions? DotnetMonitorDebug { get; set; }

    }
}
