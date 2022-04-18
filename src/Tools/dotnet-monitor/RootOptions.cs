// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal partial class RootOptions
    {
        public AuthenticationOptions Authentication { get; set; }

        public IDictionary<string, CollectionRuleOptions> CollectionRules { get; }
            = new Dictionary<string, CollectionRuleOptions>(0);

        public GlobalCounterOptions GlobalCounter { get; set; }

        public CorsConfigurationOptions CorsConfiguration { get; set; }

        public DiagnosticPortOptions DiagnosticPort { get; set; }

        public EgressOptions Egress { get; set; }

        public MetricsOptions Metrics { get; set; }

        public StorageOptions Storage { get; set; }

        public ProcessFilterOptions DefaultProcess { get; set; }

        public CollectionRuleDefaultsOptions CollectionRuleDefaults { get; set; }

        public List<CustomShortcutOptions> CustomShortcuts { get; set; }
    }
}
