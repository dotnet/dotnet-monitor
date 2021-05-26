// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.UnitTests.Options
#else
namespace Microsoft.Diagnostics.Monitoring.RestServer
#endif
{
    /// <summary>
    /// Configuration for prometheus metric collection and retrieval.
    /// TODO We may want to expose https endpoints here as well, and make port changes
    /// TODO How do we determine which process to scrape in multi-proc situations? How do we configure this
    /// for situations where the pid is not known or ambiguous?
    /// </summary>
    public class MetricsOptions
    {
        [Display(Description = "Enable or disable metrics collection.")]
        [DefaultValue(MetricsOptionsDefaults.Enabled)]
        public bool? Enabled { get; set; }

        [Display(Description = "Endpoints that expose prometheus metrics. Defaults to http://localhost:52325.")]
        public string Endpoints { get; set; }

        [DefaultValue(MetricsOptionsDefaults.UpdateIntervalSeconds)]
        [Display(Description = "How often metrics are collected.")]
        public int? UpdateIntervalSeconds { get; set; }

        [DefaultValue(MetricsOptionsDefaults.MetricCount)]
        [Display(Description = "Amount of data points to store per metric.")]
        public int? MetricCount { get; set; }

        [DefaultValue(MetricsOptionsDefaults.IncludeDefaultProviders)]
        [Display(Description = "Include default providers: System.Runtime, Microsoft.AspNetCore.Hosting, and Grpc.AspNetCore.Server.")]
        public bool? IncludeDefaultProviders { get; set; }

        [Display(Description = "Providers for custom metrics.")]
        public List<MetricProvider> Providers { get; set; } = new List<MetricProvider>(0);
    }

    public class MetricProvider
    {
        [Display(Description = "The name of the custom metrics provider.")]
        [Required]
        public string ProviderName { get; set; }

        [Display(Description = "Name of custom metrics counters.")]
        public List<string> CounterNames { get; set; } = new List<string>(0);
    }
}
