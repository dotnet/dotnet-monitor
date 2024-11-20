// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
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
                code.Add(_metric.CounterMetadata.ProviderName);
                code.Add(_metric.CounterMetadata.CounterName);
                return code.ToHashCode();
            }

            public override bool Equals(object? obj)
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
        private ILogger<MetricsStoreService> _logger;

        private HashSet<string> _observedErrorMessages = new();
        private HashSet<(string provider, string counter)> _observedEndedCounters = new();

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
            if (metric is AggregatePercentilePayload payload && !payload.Quantiles.Any())
            {
                // If histogram data is not generated in the monitored app, we can get Histogram events that do not contain quantiles.
                // For now, we will ignore these events.
                return;
            }
            //Do not accept CounterEnded payloads.
            if (metric is CounterEndedPayload counterEnded)
            {
                if (_observedEndedCounters.Add((counterEnded.CounterMetadata.ProviderName, counterEnded.CounterMetadata.CounterName)))
                {
                    _logger.CounterEndedPayload(counterEnded.CounterMetadata.CounterName);
                }
                return;
            }
            if (metric is ErrorPayload errorPayload)
            {
                if (_observedErrorMessages.Add(errorPayload.ErrorMessage))
                {
                    // We only show unique errors once. For example, if a rate callback throws an exception,
                    // we will receive an error message every 5 seconds. However, we only log the message the first time.
                    // Error payload information is not tied to a particular provider or counter name.
                    _logger.ErrorPayload(errorPayload.ErrorMessage);
                }
                return;
            }
            if (!metric.EventType.IsValuePublishedEvent())
            {
                // Do we want to do anything with this payload?
                return;
            }

            lock (_allMetrics)
            {
                var metricKey = new MetricKey(metric);
                if (!_allMetrics.TryGetValue(metricKey, out Queue<ICounterPayload>? metrics))
                {
                    metrics = new Queue<ICounterPayload>();
                    _allMetrics.Add(metricKey, metrics);
                }
                metrics.Enqueue(metric);
                if (metrics.Count > _maxMetricCount)
                {
                    metrics.Dequeue();
                }

                // CONSIDER We only keep 1 histogram representation per snapshot. Is it meaningful for Prometheus to see previous histograms? These are not timestamped.
                if ((metrics.Count > 1) && (metric is AggregatePercentilePayload))
                {
                    metrics.Dequeue();
                }
            }
        }

        public async Task SnapshotMetrics(Stream outputStream, CancellationToken token)
        {
            Dictionary<MetricKey, Queue<ICounterPayload>>? copy = null;
            lock (_allMetrics)
            {
                copy = new Dictionary<MetricKey, Queue<ICounterPayload>>();
                foreach (var metricGroup in _allMetrics)
                {
                    copy.Add(metricGroup.Key, new Queue<ICounterPayload>(metricGroup.Value));
                }
            }

            await using var writer = new StreamWriter(outputStream, EncodingCache.UTF8NoBOMNoThrow, bufferSize: 1024, leaveOpen: true);
            writer.NewLine = "\n";

            foreach (var metricGroup in copy)
            {
                ICounterPayload metricInfo = metricGroup.Value.First();

                string metricName = PrometheusDataModel.GetPrometheusNormalizedName(metricInfo.CounterMetadata.ProviderName, metricInfo.CounterMetadata.CounterName, metricInfo.Unit);

                await WriteMetricHeader(metricInfo, writer, metricName);

                foreach (var metric in metricGroup.Value)
                {
                    if (metric is AggregatePercentilePayload aggregatePayload)
                    {
                        // Summary quantiles must appear from smallest to largest
                        foreach (Quantile quantile in aggregatePayload.Quantiles.OrderBy(q => q.Percentage))
                        {
                            string metricValue = PrometheusDataModel.GetPrometheusNormalizedValue(metric.Unit, quantile.Value);
                            string metricLabels = GetMetricLabels(metric, quantile.Percentage);
                            await WriteMetricDetails(writer, metric, metricName, metricValue, metricLabels);
                        }
                    }
                    else
                    {
                        string metricValue = PrometheusDataModel.GetPrometheusNormalizedValue(metric.Unit, metric.Value);
                        string metricLabels = GetMetricLabels(metric, quantile: null);
                        await WriteMetricDetails(writer, metric, metricName, metricValue, metricLabels);
                    }
                }
            }
        }

        private static string GetMetricLabels(ICounterPayload metric, double? quantile)
        {
            string allMetadata = metric.CombineTags();

            char separator = IsMeter(metric) ? '=' : ':';
            var metadataValues = CounterUtilities.GetMetadata(allMetadata, separator);
            if (quantile.HasValue)
            {
                metadataValues.Add("quantile", quantile.Value.ToString(CultureInfo.InvariantCulture));
            }

            var keyValuePairs = from pair in metadataValues
                                select PrometheusDataModel.GetPrometheusNormalizedLabel(pair.Key, pair.Value);

            string metricLabels = string.Join(", ", keyValuePairs);

            return metricLabels;
        }

        //HACK We should make this easier in the base api
        private static bool IsMeter(ICounterPayload payload) =>
            payload switch
            {
                GaugePayload or PercentilePayload or CounterEndedPayload or RatePayload or AggregatePercentilePayload or UpDownCounterPayload => true,
                _ => false
            };

        private static async Task WriteMetricHeader(ICounterPayload metricInfo, StreamWriter writer, string metricName)
        {
            if ((!metricInfo.EventType.IsError()) && (metricInfo.EventType != EventType.CounterEnded))
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
                // In Prometheus, rates are treated as gauges due to their non-monotonic nature
                case EventType.Rate:
                case EventType.UpDownCounter:
                case EventType.Gauge:
                    return "gauge";
                case EventType.Histogram:
                    return "summary";
                case EventType.HistogramLimitError:
                case EventType.TimeSeriesLimitError:
                case EventType.ErrorTargetProcess:
                case EventType.MultipleSessionsNotSupportedError:
                case EventType.MultipleSessionsConfiguredIncorrectlyError:
                case EventType.ObservableInstrumentCallbackError:
                default:
                    return string.Empty; // Not sure this is how we want to do it.
            }
        }

        private static async Task WriteMetricDetails(
                    StreamWriter writer,
                    ICounterPayload metric,
                    string metricName,
                    string metricValue,
                    string metricLabels)
        {
            await writer.WriteAsync(metricName);
            if (!string.IsNullOrWhiteSpace(metricLabels))
            {
                await writer.WriteAsync("{" + metricLabels + "}");
            }

            string lineSuffix = metric is AggregatePercentilePayload ? string.Empty : FormattableString.Invariant($" {new DateTimeOffset(metric.Timestamp).ToUnixTimeMilliseconds()}");

            await writer.WriteLineAsync(FormattableString.Invariant($" {metricValue}{lineSuffix}"));
        }

        private static bool CompareMetrics(ICounterPayload first, ICounterPayload second)
        {
            return string.Equals(first.CounterMetadata.CounterName, second.CounterMetadata.CounterName);
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
