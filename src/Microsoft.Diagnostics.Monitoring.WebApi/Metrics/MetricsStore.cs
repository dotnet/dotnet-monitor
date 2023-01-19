﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private Dictionary<MetricKey, Queue<List<ICounterPayload>>> _allMetrics = new Dictionary<MetricKey, Queue<List<ICounterPayload>>>();
        private readonly int _maxMetricCount;
        private ILogger<MetricsStoreService> _logger;

        public MetricsStore(ILogger<MetricsStoreService> logger, int maxMetricCount)
        {
            if (maxMetricCount < 1)
            {
                throw new ArgumentException(Strings.ErrorMessage_InvalidMetricCount);
            }
            _maxMetricCount = maxMetricCount;
            _logger = logger;
        }

        public void AddMetric(ICounterPayload metric)
        {
            List<ICounterPayload> metricAsList = new();
            metricAsList.Add(metric);

            lock (_allMetrics)
            {
                var metricKey = new MetricKey(metric);

                if (metric.EventType == EventType.Histogram)
                {
                    if (_allMetrics.TryGetValue(metricKey, out Queue<List<ICounterPayload>> metrics))
                    {
                        List<ICounterPayload> recentMetrics = metrics.LastOrDefault();

                        if (recentMetrics.Any() && recentMetrics[0].Timestamp == metric.Timestamp)
                        {
                            metrics.LastOrDefault().Add(metric);
                        }
                        else
                        {
                            metrics.Enqueue(metricAsList);
                        }
                    }
                    else
                    {
                        metrics = new Queue<List<ICounterPayload>>();
                        _allMetrics.Add(metricKey, metrics);
                        metrics.Enqueue(metricAsList);
                    }

                    if (metrics.Count > _maxMetricCount)
                    {
                        metrics.Dequeue();
                    }
                }
                else
                {
                    if (!_allMetrics.TryGetValue(metricKey, out Queue<List<ICounterPayload>> metrics))
                    {
                        metrics = new Queue<List<ICounterPayload>>();
                        _allMetrics.Add(metricKey, metrics);
                    }
                    metrics.Enqueue(metricAsList);
                    if (metrics.Count > _maxMetricCount)
                    {
                        metrics.Dequeue();
                    }
                }
            }
        }

        public async Task SnapshotMetrics(Stream outputStream, CancellationToken token)
        {
            Dictionary<MetricKey, Queue<List<ICounterPayload>>> copy = null;
            lock (_allMetrics)
            {
                copy = new Dictionary<MetricKey, Queue<List<ICounterPayload>>>();
                foreach (var metricGroup in _allMetrics)
                {
                    copy.Add(metricGroup.Key, new Queue<List<ICounterPayload>>(metricGroup.Value));
                }
            }

            await using var writer = new StreamWriter(outputStream, EncodingCache.UTF8NoBOMNoThrow, bufferSize: 1024, leaveOpen: true);
            writer.NewLine = "\n";

            foreach (var metricGroup in copy)
            {
                List<ICounterPayload> metricInfo = metricGroup.Value.First();

                string metricName = PrometheusDataModel.GetPrometheusNormalizedName(metricInfo[0].Provider, metricInfo[0].Name, metricInfo[0].Unit);

                await WriteMetricHeader(metricInfo[0], writer, metricName);

                foreach (var metric in metricGroup.Value)
                {
                    foreach (var individualMetric in metric)
                    {
                        char separator = IsMeter(individualMetric) ? '=' : ':';
                        var keyValuePairs = from pair in Microsoft.Diagnostics.Monitoring.EventPipe.TraceEventExtensions.GetMetadata(individualMetric.Metadata, separator)
                                            select pair.Key + "=" + "\"" + pair.Value + "\"";
                        string metricLabels = string.Join(", ", keyValuePairs);

                        string metricValue = PrometheusDataModel.GetPrometheusNormalizedValue(individualMetric.Unit, individualMetric.Value);
                        await WriteMetricDetails(writer, individualMetric, metricName, metricValue, metricLabels);
                    }
                }
            }
        }

        //HACK We should make this easier in the base api
        private static bool IsMeter(ICounterPayload payload) =>
            payload switch
            {
                GaugePayload or PercentilePayload or CounterEndedPayload or RatePayload => true,
                _ => false
            };

        private static async Task WriteMetricHeader(ICounterPayload metricInfo, StreamWriter writer, string metricName)
        {
            if (metricInfo.EventType != EventType.Error)
            {
                string metricType = GetMetricType(metricInfo.EventType);

                await writer.WriteLineAsync(FormattableString.Invariant($"# HELP {metricName} {metricInfo.DisplayName}"));
                await writer.WriteLineAsync(FormattableString.Invariant($"# TYPE {metricName} {metricType}"));
            }
        }

        private static string GetMetricType(EventType eventType)
        {
            switch (eventType)
            {
                case EventType.Rate:
                    return "counter";
                case EventType.Gauge:
                    return "gauge";
                case EventType.Histogram:
                    return "summary";
                case EventType.Error:
                default:
                    return string.Empty; // Not sure this is how we want to do it.
            }
        }

        private async Task WriteMetricDetails(
                    StreamWriter writer,
                    ICounterPayload metric,
                    string metricName,
                    string metricValue,
                    string metricLabels)
        {
            if (metric is GaugePayload)
            {
                await writer.WriteAsync(metricName);
                if (!string.IsNullOrWhiteSpace(metricLabels))
                {
                    await writer.WriteAsync("{" + metricLabels + "}");
                }
                await writer.WriteLineAsync(FormattableString.Invariant($" {metricValue} {new DateTimeOffset(metric.Timestamp).ToUnixTimeMilliseconds()}"));
            }
            else if (metric is RatePayload)
            {
                await writer.WriteAsync(metricName);
                if (!string.IsNullOrWhiteSpace(metricLabels))
                {
                    await writer.WriteAsync("{" + metricLabels + "}");
                }
                await writer.WriteLineAsync(FormattableString.Invariant($" {metricValue} {new DateTimeOffset(metric.Timestamp).ToUnixTimeMilliseconds()}"));
            }
            else if (metric is PercentilePayload)
            {
                await writer.WriteAsync(metricName); // Just experimenting with this
                if (!string.IsNullOrWhiteSpace(metricLabels))
                {
                    await writer.WriteAsync("{" + metricLabels + "}");
                }
                await writer.WriteLineAsync(FormattableString.Invariant($" {metricValue}"));
            }
            else if (metric is ErrorPayload errorMetric)
            {
                _logger.LogWarning(errorMetric.ErrorMessage);
            }
            else if (metric is CounterEndedPayload)
            {
                _logger.CounterEndedPayload(metric.Name);
            }
            else
            {
                // Existing format for EventCounters
                await writer.WriteAsync(metricName);
                if (!string.IsNullOrWhiteSpace(metricLabels))
                {
                    await writer.WriteAsync("{" + metricLabels + "}");
                }
                await writer.WriteLineAsync(FormattableString.Invariant($" {metricValue} {new DateTimeOffset(metric.Timestamp).ToUnixTimeMilliseconds()}"));
            }
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
