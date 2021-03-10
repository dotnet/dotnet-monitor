// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.RestServer
{
    /// <summary>
    /// Configuration for prometheus metric collection and retrieval.
    /// TODO We may want to expose https endpoints here as well, and make port changes
    /// TODO How do we determine which process to scrape in multi-proc situations? How do we configure this
    /// for situations where the pid is not known or ambiguous?
    /// </summary>
    public class MetricsOptions
    {
        public const string ConfigurationKey = "Metrics";

        public bool Enabled { get; set; }
        
        public string Endpoints { get; set; }

        public int UpdateIntervalSeconds { get; set; }

        public int MetricCount { get; set; }

        public bool IncludeDefaultProviders { get; set; } = true;

        public bool AllowInsecureChannelForCustomMetrics { get; set; } = false;

        public List<MetricProvider> Providers { get; set; } = new List<MetricProvider>(0);
    }

    public class MetricProvider
    {
        public string ProviderName { get; set; }
        public List<string> CounterNames { get; set; } = new List<string>(0);
    }
}
