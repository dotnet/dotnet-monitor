// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Used to store metrics. A snapshot will be requested periodically.
    /// </summary>
    internal interface IMetricsStore : IDisposable
    {
        void AddMetric(ICounterPayload metric);

        Task SnapshotMetrics(Stream stream, CancellationToken token);

        void SnapshotMetrics(out MetricsSnapshot snapshot, bool deltaAggregation = false);

        void Clear();
    }

    internal sealed class MetricsSnapshot
    {
        public DateTime ProcessStartTimeUtc { get; }

        public DateTime LastCollectionStartTimeUtc { get; }

        public DateTime LastCollectionEndTimeUtc { get; }

        public IReadOnlyList<MetricsSnapshotMeter> Meters { get; }

        public MetricsSnapshot(
            DateTime processStartTimeUtc,
            DateTime lastCollectionStartTimeUtc,
            DateTime lastCollectionEndTimeUtc,
            IReadOnlyList<MetricsSnapshotMeter> meters)
        {
            ProcessStartTimeUtc = processStartTimeUtc;
            LastCollectionStartTimeUtc = lastCollectionStartTimeUtc;
            LastCollectionEndTimeUtc = lastCollectionEndTimeUtc;
            Meters = meters;
        }
    }

    internal sealed class MetricsSnapshotMeter
    {
        public string MeterName { get; }

        public string MeterVersion { get; }

        public IReadOnlyList<MetricsSnapshotInstrument> Instruments { get; }

        public MetricsSnapshotMeter(
            string meterName,
            string meterVersion,
            IReadOnlyList<MetricsSnapshotInstrument> instruments)
        {
            MeterName = meterName;
            MeterVersion = meterVersion;
            Instruments = instruments;
        }
    }

    internal sealed class MetricsSnapshotInstrument
    {
        public CounterMetadata Metadata { get; }

        public IReadOnlyList<ICounterPayload> MetricPoints { get; }

        public MetricsSnapshotInstrument(
            CounterMetadata metadata,
            IReadOnlyList<ICounterPayload> metricPoints)
        {
            Metadata = metadata;
            MetricPoints = metricPoints;
        }
    }
}
