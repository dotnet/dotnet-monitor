// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal sealed class MetricsStoreService : IDisposable
    {
        public MetricsStore MetricsStore { get; }

        public MetricsStoreService(
            ILogger<MetricsStoreService> logger,
            IOptions<MetricsOptions> options)
        {
            MetricsStore = new MetricsStore(logger, options.Value.MetricCount.GetValueOrDefault(MetricsOptionsDefaults.MetricCount));
        }

        public void Dispose()
        {
            MetricsStore.Dispose();
        }
    }
}
