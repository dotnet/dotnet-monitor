// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if BUILDING_OTEL
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;

using OTel = OpenTelemetry.Metrics;

namespace Microsoft.Diagnostics.Tools.Monitor.OpenTelemetry.Metrics;

internal sealed class OpenTelemetryMetricProducer : OTel.MetricProducer
{
    private readonly MetricsStore _MetricsStore;
    private readonly OTel.AggregationTemporality _AggregationTemporality;

    public OpenTelemetryMetricProducer(
        MetricsStore metricsStore,
        OTel.AggregationTemporality aggregationTemporality)
    {
        Debug.Assert(metricsStore != null);

        _MetricsStore = metricsStore;
        _AggregationTemporality = aggregationTemporality;
    }

    public override bool WriteTo(OTel.MetricWriter writer)
    {
        OTel.AggregationTemporality aggregationTemporality = _AggregationTemporality;

        _MetricsStore.SnapshotMetrics(
            out var snapshot,
            deltaAggregation: aggregationTemporality == OTel.AggregationTemporality.Delta);

        foreach (var meter in snapshot.Meters)
        {
            writer.BeginInstrumentationScope(
                new(meter.MeterName)
                {
                    Version = meter.MeterVersion
                });

            foreach (var instrument in meter.Instruments)
            {
                OTel.Metric? otelMetric = null;

                foreach (var metricPoint in instrument.MetricPoints)
                {
                    if (otelMetric == null)
                    {
                        switch (metricPoint.EventType)
                        {
                            case EventType.Rate:
                                otelMetric = new OTel.Metric(
                                    OTel.MetricType.DoubleSum,
                                    instrument.Metadata.CounterName,
                                    aggregationTemporality)
                                {
                                    Unit = instrument.Metadata.CounterUnit,
                                    Description = instrument.Metadata.CounterDescription
                                };
                                break;
                            case EventType.Gauge:
                                otelMetric = new OTel.Metric(
                                    OTel.MetricType.DoubleGauge,
                                    instrument.Metadata.CounterName,
                                    OTel.AggregationTemporality.Cumulative)
                                {
                                    Unit = instrument.Metadata.CounterUnit,
                                    Description = instrument.Metadata.CounterDescription
                                };
                                break;
                            case EventType.UpDownCounter:
                                otelMetric = new OTel.Metric(
                                    OTel.MetricType.DoubleSumNonMonotonic,
                                    instrument.Metadata.CounterName,
                                    OTel.AggregationTemporality.Cumulative)
                                {
                                    Unit = instrument.Metadata.CounterUnit,
                                    Description = instrument.Metadata.CounterDescription
                                };
                                break;
                            case EventType.Histogram:
                                otelMetric = new OTel.Metric(
                                    OTel.MetricType.Histogram,
                                    instrument.Metadata.CounterName,
                                    aggregationTemporality)
                                {
                                    Unit = instrument.Metadata.CounterUnit,
                                    Description = instrument.Metadata.CounterDescription
                                };
                                break;
                            default:
                                return false;
                        }

                        writer.BeginMetric(otelMetric);
                    }

                    DateTime startTimeUtc = otelMetric.AggregationTemporality == OTel.AggregationTemporality.Cumulative
                        ? snapshot.ProcessStartTimeUtc
                        : snapshot.LastCollectionStartTimeUtc;
                    DateTime endTimeUtc = snapshot.LastCollectionEndTimeUtc;

                    switch (otelMetric.MetricType)
                    {
                        case OTel.MetricType.DoubleSum:
                        case OTel.MetricType.DoubleGauge:
                        case OTel.MetricType.DoubleSumNonMonotonic:
                            WriteNumberMetricPoint(writer, startTimeUtc, endTimeUtc, metricPoint);
                            break;
                        case OTel.MetricType.Histogram:
                            if (metricPoint is AggregatePercentilePayload aggregatePercentilePayload)
                            {
                                WriteHistogramMetricPoint(writer, startTimeUtc, endTimeUtc, aggregatePercentilePayload);
                            }
                            break;
                    }
                }

                if (otelMetric != null)
                {
                    writer.EndMetric();
                }
            }

            writer.EndInstrumentationScope();
        }

        return true;
    }

    private static void WriteNumberMetricPoint(
        OTel.MetricWriter writer,
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        ICounterPayload payload)
    {
        double value = payload is IRatePayload ratePayload
            ? ratePayload.Rate
            : payload.Value;

        var numberMetricPoint = new OTel.NumberMetricPoint(
            startTimeUtc,
            endTimeUtc,
            value);

        writer.WriteNumberMetricPoint(
            in numberMetricPoint,
            ParseAttributes(payload),
            exemplars: default);
    }

    private static void WriteHistogramMetricPoint(
        OTel.MetricWriter writer,
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        AggregatePercentilePayload payload)
    {
        var histogramMetricPoint = new OTel.HistogramMetricPoint(
            startTimeUtc,
            endTimeUtc,
            features: OTel.HistogramMetricPointFeatures.None,
            min: default,
            max: default,
            payload.Sum,
            payload.Count);

        writer.WriteHistogramMetricPoint(
            in histogramMetricPoint,
            buckets: default,
            ParseAttributes(payload),
            exemplars: default);
    }

    private static ReadOnlySpan<KeyValuePair<string, object?>> ParseAttributes(ICounterPayload payload)
    {
        List<KeyValuePair<string, object?>> attributes = new List<KeyValuePair<string, object?>>();

        ReadOnlySpan<char> metadata = payload.ValueTags;

        while (!metadata.IsEmpty)
        {
            int commaIndex = metadata.IndexOf(',');

            ReadOnlySpan<char> kvPair;

            if (commaIndex < 0)
            {
                kvPair = metadata;
                metadata = default;
            }
            else
            {
                kvPair = metadata[..commaIndex];
                metadata = metadata.Slice(commaIndex + 1);
            }

            int colonIndex = kvPair.IndexOf('=');
            if (colonIndex < 0)
            {
                attributes.Clear();
                break;
            }

            string metadataKey = kvPair[..colonIndex].ToString();
            string metadataValue = kvPair.Slice(colonIndex + 1).ToString();
            attributes.Add(new(metadataKey, metadataValue));
        }

        return CollectionsMarshal.AsSpan(attributes);
    }
}
#endif
