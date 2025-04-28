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
                code.Add(_metric.ValueTags);
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

        private readonly Dictionary<MetricKey, Queue<ICounterPayload>> _allMetrics = new Dictionary<MetricKey, Queue<ICounterPayload>>();
        private readonly int _maxMetricCount;
        private readonly ILogger<MetricsStoreService> _logger;
        private static readonly DateTime s_processStartTimeUtc = DateTime.UtcNow;
        private DateTime _lastCollectionStartTimeUtc = s_processStartTimeUtc;

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
                    if (_allMetrics.Count > _maxMetricCount)
                    {
                        return;
                    }

                    metrics = new Queue<ICounterPayload>();
                    _allMetrics.Add(metricKey, metrics);
                }
                metrics.Enqueue(metric);
            }
        }

        public void SnapshotMetrics(out MetricsSnapshot snapshot, bool deltaAggregation = false)
        {
            var meterLookup = new Dictionary<
                (string, string),
                Dictionary<string, (CounterMetadata, List<ICounterPayload>)>>();

            DateTime lastCollectionStartTimeUtc;
            DateTime lastCollectionEndTimeUtc;

            lock (_allMetrics)
            {
                foreach (var metricGroup in _allMetrics)
                {
                    var measurements = metricGroup.Value;
                    if (measurements.Count <= 0)
                    {
                        continue;
                    }

                    var firstMeasurement = measurements.Dequeue();

                    var metadata = firstMeasurement.CounterMetadata;

                    var meterKey = (metadata.ProviderName, metadata.ProviderVersion);
                    if (!meterLookup.TryGetValue(meterKey, out var meter))
                    {
                        meter = new();
                        meterLookup[meterKey] = meter;
                    }

                    if (!meter.TryGetValue(metadata.CounterName, out var instrument))
                    {
                        instrument = new(metadata, new());
                        meter[metadata.CounterName] = instrument;
                    }

                    if (firstMeasurement is RatePayload ratePayload)
                    {
                        if (measurements.Count > 1)
                        {
                            var rate = ratePayload.Rate;

                            foreach (var measurement in measurements)
                            {
                                if (measurement is RatePayload nextRatePayload)
                                {
                                    rate += nextRatePayload.Rate;
                                }
                            }

                            var aggregated = new RatePayload(
                                metadata,
                                displayName: null,
                                displayUnits: null,
                                firstMeasurement.ValueTags,
                                rate,
                                firstMeasurement.Interval,
                                firstMeasurement.Timestamp);

                            instrument.Item2.Add(aggregated);

                            measurements.Clear();

                            if (!deltaAggregation)
                            {
                                measurements.Enqueue(aggregated);
                            }
                        }
                        else
                        {
                            instrument.Item2.Add(firstMeasurement);
                            if (!deltaAggregation)
                            {
                                measurements.Enqueue(firstMeasurement);
                            }
                        }
                    }
                    else if (firstMeasurement is UpDownCounterPayload upDownCounterPayload)
                    {
                        if (measurements.Count > 1)
                        {
                            var rate = upDownCounterPayload.Rate;

                            foreach (var measurement in measurements)
                            {
                                if (measurement is UpDownCounterPayload nextUpDownCounterPayload)
                                {
                                    rate += nextUpDownCounterPayload.Rate;
                                }
                            }

                            var aggregated = new UpDownCounterPayload(
                                metadata,
                                displayName: null,
                                displayUnits: null,
                                firstMeasurement.ValueTags,
                                rate,
                                firstMeasurement.Value,
                                firstMeasurement.Timestamp);

                            instrument.Item2.Add(aggregated);

                            measurements.Clear();

                            if (!deltaAggregation)
                            {
                                measurements.Enqueue(aggregated);
                            }
                        }
                        else
                        {
                            instrument.Item2.Add(firstMeasurement);
                            if (!deltaAggregation)
                            {
                                measurements.Enqueue(firstMeasurement);
                            }
                        }
                    }
                    else if (firstMeasurement is AggregatePercentilePayload aggregatePercentilePayload)
                    {
                        if (measurements.Count > 1)
                        {
                            var count = aggregatePercentilePayload.Count;
                            var sum = aggregatePercentilePayload.Sum;

                            foreach (var measurement in measurements)
                            {
                                if (measurement is AggregatePercentilePayload nextAggregatePercentilePayload)
                                {
                                    count += nextAggregatePercentilePayload.Count;
                                    sum += nextAggregatePercentilePayload.Sum;
                                }
                            }

                            var aggregated = new AggregatePercentilePayload(
                                metadata,
                                displayName: null,
                                displayUnits: null,
                                firstMeasurement.ValueTags,
                                count,
                                sum,
                                aggregatePercentilePayload.Quantiles,
                                firstMeasurement.Timestamp);

                            instrument.Item2.Add(aggregated);

                            measurements.Clear();

                            if (!deltaAggregation)
                            {
                                measurements.Enqueue(aggregated);
                            }
                        }
                        else
                        {
                            instrument.Item2.Add(firstMeasurement);
                            if (!deltaAggregation)
                            {
                                measurements.Enqueue(firstMeasurement);
                            }
                        }
                    }
                    else
                    {
                        var lastMeasurement = measurements.Count > 0
                            ? measurements.Last()
                            : firstMeasurement;

                        instrument.Item2.Add(lastMeasurement);

                        if (measurements.Count > 1)
                        {
                            measurements.Clear();
                        }

                        if (!deltaAggregation)
                        {
                            measurements.Enqueue(lastMeasurement);
                        }
                    }
                }

                lastCollectionStartTimeUtc = _lastCollectionStartTimeUtc;
                lastCollectionEndTimeUtc = _lastCollectionStartTimeUtc = DateTime.UtcNow;
            }

            var meters = new List<MetricsSnapshotMeter>();
            foreach (var meter in meterLookup)
            {
                var instruments = new List<MetricsSnapshotInstrument>();
                foreach (var instrument in meter.Value)
                {
                    instruments.Add(
                        new(instrument.Value.Item1, instrument.Value.Item2));
                }

                meters.Add(
                    new(
                        meterName: meter.Key.Item1,
                        meterVersion: meter.Key.Item2,
                        instruments));
            }

            snapshot = new(s_processStartTimeUtc, lastCollectionStartTimeUtc, lastCollectionEndTimeUtc, meters);
        }

        public async Task SnapshotMetrics(Stream outputStream, CancellationToken token)
        {
            Dictionary<MetricKey, ICounterPayload> snapshot = new Dictionary<MetricKey, ICounterPayload>();
            lock (_allMetrics)
            {
                foreach (var metricGroup in _allMetrics)
                {
                    var measurements = metricGroup.Value;

                    var firstMeasurement = measurements.Dequeue();

                    if (firstMeasurement is RatePayload ratePayload)
                    {
                        if (measurements.Count > 1)
                        {
                            var rate = ratePayload.Rate;

                            foreach (var measurement in measurements)
                            {
                                if (measurement is RatePayload nextRatePayload)
                                {
                                    rate += nextRatePayload.Rate;
                                }
                            }

                            var aggregated = new RatePayload(
                                firstMeasurement.CounterMetadata,
                                displayName: null,
                                displayUnits: null,
                                firstMeasurement.ValueTags,
                                rate,
                                firstMeasurement.Interval,
                                firstMeasurement.Timestamp);

                            snapshot.Add(metricGroup.Key, aggregated);

                            measurements.Clear();

                            measurements.Enqueue(aggregated);
                        }
                        else
                        {
                            snapshot.Add(metricGroup.Key, firstMeasurement);
                            measurements.Enqueue(firstMeasurement);
                        }
                    }
                    else if (firstMeasurement is UpDownCounterPayload upDownCounterPayload)
                    {
                        if (measurements.Count > 1)
                        {
                            var rate = upDownCounterPayload.Rate;

                            foreach (var measurement in measurements)
                            {
                                if (measurement is UpDownCounterPayload nextUpDownCounterPayload)
                                {
                                    rate += nextUpDownCounterPayload.Rate;
                                }
                            }

                            var aggregated = new UpDownCounterPayload(
                                firstMeasurement.CounterMetadata,
                                displayName: null,
                                displayUnits: null,
                                firstMeasurement.ValueTags,
                                rate,
                                firstMeasurement.Value,
                                firstMeasurement.Timestamp);

                            snapshot.Add(metricGroup.Key, aggregated);

                            measurements.Clear();

                            measurements.Enqueue(aggregated);
                        }
                        else
                        {
                            snapshot.Add(metricGroup.Key, firstMeasurement);
                            measurements.Enqueue(firstMeasurement);
                        }
                    }
                    else if (firstMeasurement is AggregatePercentilePayload aggregatePercentilePayload)
                    {
                        if (measurements.Count > 1)
                        {
                            var count = aggregatePercentilePayload.Count;
                            var sum = aggregatePercentilePayload.Sum;

                            foreach (var measurement in measurements)
                            {
                                if (measurement is AggregatePercentilePayload nextAggregatePercentilePayload)
                                {
                                    count += nextAggregatePercentilePayload.Count;
                                    sum += nextAggregatePercentilePayload.Sum;
                                }
                            }

                            var aggregated = new AggregatePercentilePayload(
                                firstMeasurement.CounterMetadata,
                                displayName: null,
                                displayUnits: null,
                                firstMeasurement.ValueTags,
                                count,
                                sum,
                                aggregatePercentilePayload.Quantiles,
                                firstMeasurement.Timestamp);

                            snapshot.Add(metricGroup.Key, aggregated);

                            measurements.Clear();

                            measurements.Enqueue(aggregated);
                        }
                        else
                        {
                            snapshot.Add(metricGroup.Key, firstMeasurement);
                            measurements.Enqueue(firstMeasurement);
                        }
                    }
                    else
                    {
                        var lastMeasurement = measurements.Count > 0
                            ? measurements.Last()
                            : firstMeasurement;

                        snapshot.Add(metricGroup.Key, lastMeasurement);

                        if (measurements.Count > 1)
                        {
                            measurements.Clear();
                        }

                        measurements.Enqueue(lastMeasurement);
                    }
                }

                _allMetrics.Clear();
            }

            await using var writer = new StreamWriter(outputStream, EncodingCache.UTF8NoBOMNoThrow, bufferSize: 1024, leaveOpen: true);
            writer.NewLine = "\n";

            foreach (var metricGroup in snapshot)
            {
                ICounterPayload metric = metricGroup.Value;

                string metricName = PrometheusDataModel.GetPrometheusNormalizedName(metric.CounterMetadata.ProviderName, metric.CounterMetadata.CounterName, metric.Unit);

                await WriteMetricHeader(metric, writer, metricName);

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
