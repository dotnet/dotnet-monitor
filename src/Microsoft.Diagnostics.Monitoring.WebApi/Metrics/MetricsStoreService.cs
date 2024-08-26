// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal sealed class MetricsStoreService : IDisposable
    {
        private readonly ILogger<MetricsStoreService> _logger;
        private readonly IOptions<MetricsOptions> _options;
        private readonly IDictionary<int, MetricsStore> _metricStores = new Dictionary<int, MetricsStore>();

        public MetricsStore MetricsStore { get; }

        public MetricsStoreService(
            ILogger<MetricsStoreService> logger,
            IOptions<MetricsOptions> options)
        {
            _logger = logger;
            _options = options;
            MetricsStore = CreateMetricsStore(defaultLabels: null);
        }

        private MetricsStore CreateMetricsStore(IDictionary<string, string>? defaultLabels)
            => new (_logger, _options.Value.MetricCount.GetValueOrDefault(MetricsOptionsDefaults.MetricCount), defaultLabels);

        public MetricsStore GetOrCreateStoreFor(IProcessInfo process)
        {
            lock (_metricStores)
            {
                int key = process.EndpointInfo.ProcessId;
                if (_metricStores.TryGetValue(key, out MetricsStore? store))
                {
                    return store;
                }

                IDictionary<string, string> processLabels = GetProcessLabels(process);
                store = CreateMetricsStore(processLabels);
                _metricStores.Add(key, store);
                return store;
            }
        }

        public bool RemoveMetricsForPid(int pid)
        {
            lock (_metricStores)
            {
                return _metricStores.Remove(pid);
            }
        }

        public IEnumerable<MetricsStore> GetAllMetrics()
        {
            MetricsStore[] all;
            lock (_metricStores)
            {
                all = new MetricsStore[_metricStores.Count];
                _metricStores.Values.CopyTo(all, 0);
            }

            return all;
        }
        private static IDictionary<string, string> GetProcessLabels(IProcessInfo process)
        {
            return new Dictionary<string, string>()
            {
                { "process_id", process.EndpointInfo.ProcessId.ToString() },
                { "process_name", process.GetProcessName() },
            };
        }

        

        public void Dispose()
        {
            MetricsStore.Dispose();
        }
    }
}
