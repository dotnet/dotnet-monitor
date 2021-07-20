// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Stores metrics, and produces a snapshot in Prometheus exposition format.
    /// </summary>
    internal sealed class MetricsStore : IMetricsStore
    {
        private sealed class MetricKey
        {
            private ICounterPayload _metric;

            public MetricKey(ICounterPayload metric)
            {
                _metric = metric;
            }

            public override int GetHashCode()
            {
                HashCode code = new HashCode();
                code.Add(_metric.Provider);
                code.Add(_metric.Name);
                return code.ToHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj is MetricKey metricKey)
                {
                    return CompareMetrics(_metric, metricKey._metric);
                }
                return false;
            }
        }

        private Dictionary<MetricKey, Queue<ICounterPayload>> _allMetrics = new Dictionary<MetricKey, Queue<ICounterPayload>>();
        private readonly int _maxMetricCount;

        public MetricsStore(int maxMetricCount)
        {
            if (maxMetricCount < 1)
            {
                throw new ArgumentException(Strings.ErrorMessage_InvalidMetricCount);
            }
            _maxMetricCount = maxMetricCount;
        }

        public void AddMetric(ICounterPayload metric)
        {
            lock (_allMetrics)
            {
                var metricKey = new MetricKey(metric);
                if (!_allMetrics.TryGetValue(metricKey, out Queue<ICounterPayload> metrics))
                {
                    metrics = new Queue<ICounterPayload>();
                    _allMetrics.Add(metricKey, metrics);
                }
                metrics.Enqueue(metric);
                if (metrics.Count > _maxMetricCount)
                {
                    metrics.Dequeue();
                }
            }
        }

        public async Task SnapshotMetrics(Stream outputStream, CancellationToken token)
        {
            Dictionary<MetricKey, Queue<ICounterPayload>> copy = null;
            lock (_allMetrics)
            {
                copy = new Dictionary<MetricKey, Queue<ICounterPayload>>();
                foreach (var metricGroup in _allMetrics)
                {
                    copy.Add(metricGroup.Key, new Queue<ICounterPayload>(metricGroup.Value));
                }
            }

            using var writer = new StreamWriter(outputStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 1024, leaveOpen: true);
            writer.NewLine = "\n";

            foreach (var metricGroup in copy)
            {
                ICounterPayload metricInfo = metricGroup.Value.First();
                string metricName = PrometheusDataModel.Normalize(metricInfo.Provider, metricInfo.Name,
                    metricInfo.Unit, metricInfo.Value, out string metricValue);

                string metricType = "gauge";

                //TODO Some clr metrics claim to be incrementing, but are really gauges.

                await writer.WriteLineAsync(FormattableString.Invariant($"# HELP {metricName} {metricInfo.DisplayName}"));
                await writer.WriteLineAsync(FormattableString.Invariant($"# TYPE {metricName} {metricType}"));

                foreach (var metric in metricGroup.Value)
                {
                    await WriteMetricDetails(writer, metric, metricName, metricValue);
                }
            }
        }

        private static async Task WriteMetricDetails(
                    StreamWriter writer,
                    ICounterPayload metric,
                    string metricName,
                    string metricValue)
        {
            await writer.WriteAsync(metricName);
            await writer.WriteLineAsync(FormattableString.Invariant($" {metricValue} {new DateTimeOffset(metric.Timestamp).ToUnixTimeMilliseconds()}"));
        }

        private static bool CompareMetrics(ICounterPayload first, ICounterPayload second)
        {
            return string.Equals(first.Name, second.Name);
        }

        public void Clear()
        {
            lock (_allMetrics)
            {
                _allMetrics.Clear();
            }
        }

        public void Dispose()
        {
        }
    }
}
