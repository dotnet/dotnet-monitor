// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class MetricsFormattingTests
    {
        private ITestOutputHelper _outputHelper;

        private readonly string MeterName = "MeterName";
        private readonly string InstrumentName = "InstrumentName";
        private readonly DateTime Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(10000000).DateTime;
        private readonly ILogger<MetricsStoreService> _logger;

        private const int MetricCount = 1;
        private const int Value1 = 1;
        private const int Value2 = 2;
        private const int Value3 = 3;
        private const int IntervalSeconds = 10;

        public MetricsFormattingTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;

            LoggerFactory factory = new LoggerFactory();
            _logger = factory.CreateLogger<MetricsStoreService>();
        }

        [Fact]
        public async Task HistogramFormat_Test()
        {
            List<ICounterPayload> payload = new();

            CounterMetadata counterInfo = new CounterMetadata(MeterName, InstrumentName, meterTags: null, instrumentTags: null, scopeHash: null);

            payload.Add(new AggregatePercentilePayload(counterInfo, "DisplayName", string.Empty, string.Empty,
                new Quantile[] { new Quantile(0.5, Value1), new Quantile(0.95, Value2), new Quantile(0.99, Value3) },
                Timestamp));

            using MemoryStream stream = await GetMetrics(payload);
            List<string> lines = ReadStream(stream);

            string metricName = $"{MeterName.ToLowerInvariant()}_{payload[0].CounterMetadata.CounterName}";

            const string quantile_50 = "{quantile=\"0.5\"}";
            const string quantile_95 = "{quantile=\"0.95\"}";
            const string quantile_99 = "{quantile=\"0.99\"}";

            Assert.Equal(5, lines.Count);
            Assert.Equal(FormattableString.Invariant($"# HELP {metricName}{payload[0].Unit} {payload[0].DisplayName}"), lines[0]);
            Assert.Equal(FormattableString.Invariant($"# TYPE {metricName} summary"), lines[1]);
            Assert.Equal(FormattableString.Invariant($"{metricName}{quantile_50} {Value1}"), lines[2]);
            Assert.Equal(FormattableString.Invariant($"{metricName}{quantile_95} {Value2}"), lines[3]);
            Assert.Equal(FormattableString.Invariant($"{metricName}{quantile_99} {Value3}"), lines[4]);
        }

        [Fact]
        public async Task HistogramFormat_Test_Tags()
        {
            List<ICounterPayload> payload = new();

            string meterTags = "MeterTagKey=MeterTagValue,MeterTagKey2=MeterTagValue2";
            string instrumentTags = "InstrumentTagKey=InstrumentTagValue,InstrumentTagKey2=InstrumentTagValue2";

            CounterMetadata counterInfo = new CounterMetadata(MeterName, InstrumentName, meterTags: meterTags, instrumentTags: instrumentTags, scopeHash: null);

            payload.Add(new AggregatePercentilePayload(counterInfo, "DisplayName", string.Empty, string.Empty,
                new Quantile[] { new Quantile(0.5, Value1), new Quantile(0.95, Value2), new Quantile(0.99, Value3) },
                Timestamp));

            using MemoryStream stream = await GetMetrics(payload);
            List<string> lines = ReadStream(stream);

            string metricName = $"{MeterName.ToLowerInvariant()}_{payload[0].CounterMetadata.CounterName}";

            const string quantile_50 = "{MeterTagKey=\"MeterTagValue\", MeterTagKey2=\"MeterTagValue2\", InstrumentTagKey=\"InstrumentTagValue\", InstrumentTagKey2=\"InstrumentTagValue2\", quantile=\"0.5\"}";
            const string quantile_95 = "{MeterTagKey=\"MeterTagValue\", MeterTagKey2=\"MeterTagValue2\", InstrumentTagKey=\"InstrumentTagValue\", InstrumentTagKey2=\"InstrumentTagValue2\", quantile=\"0.95\"}";
            const string quantile_99 = "{MeterTagKey=\"MeterTagValue\", MeterTagKey2=\"MeterTagValue2\", InstrumentTagKey=\"InstrumentTagValue\", InstrumentTagKey2=\"InstrumentTagValue2\", quantile=\"0.99\"}";

            Assert.Equal(5, lines.Count);
            Assert.Equal(FormattableString.Invariant($"# HELP {metricName}{payload[0].Unit} {payload[0].DisplayName}"), lines[0]);
            Assert.Equal(FormattableString.Invariant($"# TYPE {metricName} summary"), lines[1]);
            Assert.Equal(FormattableString.Invariant($"{metricName}{quantile_50} {Value1}"), lines[2]);
            Assert.Equal(FormattableString.Invariant($"{metricName}{quantile_95} {Value2}"), lines[3]);
            Assert.Equal(FormattableString.Invariant($"{metricName}{quantile_99} {Value3}"), lines[4]);
        }

        [Fact]
        public async Task GaugeFormat_Test()
        {
            CounterMetadata counterInfo = new CounterMetadata(MeterName, InstrumentName, meterTags: null, instrumentTags: null, scopeHash: null);

            ICounterPayload payload = new GaugePayload(counterInfo, "DisplayName", "", null, Value1, Timestamp);

            MemoryStream stream = await GetMetrics(new() { payload });

            List<string> lines = ReadStream(stream);

            string metricName = $"{MeterName.ToLowerInvariant()}_{payload.CounterMetadata.CounterName}";

            Assert.Equal(3, lines.Count);
            Assert.Equal(FormattableString.Invariant($"# HELP {metricName}{payload.Unit} {payload.DisplayName}"), lines[0]);
            Assert.Equal(FormattableString.Invariant($"# TYPE {metricName} gauge"), lines[1]);
            Assert.Equal(FormattableString.Invariant($"{metricName} {payload.Value} {new DateTimeOffset(payload.Timestamp).ToUnixTimeMilliseconds()}"), lines[2]);
        }

        [Fact]
        public async Task CounterFormat_Test()
        {
            CounterMetadata counterInfo = new CounterMetadata(MeterName, InstrumentName, meterTags: null, instrumentTags: null, scopeHash: null);

            ICounterPayload payload = new RatePayload(counterInfo, "DisplayName", "", null, Value1, IntervalSeconds, Timestamp);

            MemoryStream stream = await GetMetrics(new() { payload });

            List<string> lines = ReadStream(stream);

            string metricName = $"{MeterName.ToLowerInvariant()}_{payload.CounterMetadata.CounterName}";

            Assert.Equal(3, lines.Count);
            Assert.Equal($"# HELP {metricName}{payload.Unit} {payload.DisplayName}", lines[0]);
            Assert.Equal($"# TYPE {metricName} gauge", lines[1]);
            Assert.Equal($"{metricName} {payload.Value} {new DateTimeOffset(payload.Timestamp).ToUnixTimeMilliseconds()}", lines[2]);
        }

        [Fact]
        public async Task CounterFormat_Test_Tags()
        {
            string meterTags = "MeterTagKey=MeterTagValue,MeterTagKey2=MeterTagValue2";
            string instrumentTags = "InstrumentTagKey=InstrumentTagValue,InstrumentTagKey2=InstrumentTagValue2";
            string scopeHash = "123";

            CounterMetadata counterInfo = new CounterMetadata(MeterName, InstrumentName, meterTags, instrumentTags, scopeHash);

            ICounterPayload payload = new RatePayload(counterInfo, "DisplayName", "", null, Value1, IntervalSeconds, Timestamp);

            MemoryStream stream = await GetMetrics(new() { payload });

            List<string> lines = ReadStream(stream);

            string metricName = $"{MeterName.ToLowerInvariant()}_{payload.CounterMetadata.CounterName}";
            string metricTags = "{MeterTagKey=\"MeterTagValue\", MeterTagKey2=\"MeterTagValue2\", InstrumentTagKey=\"InstrumentTagValue\", InstrumentTagKey2=\"InstrumentTagValue2\"}";

            Assert.Equal(3, lines.Count);
            Assert.Equal($"# HELP {metricName}{payload.Unit} {payload.DisplayName}", lines[0]);
            Assert.Equal($"# TYPE {metricName} gauge", lines[1]);
            Assert.Equal($"{metricName}{metricTags} {payload.Value} {new DateTimeOffset(payload.Timestamp).ToUnixTimeMilliseconds()}", lines[2]);
        }

        [Fact]
        public async Task UpDownCounterFormat_Test()
        {
            CounterMetadata counterInfo = new CounterMetadata(MeterName, InstrumentName, meterTags: null, instrumentTags: null, scopeHash: null);

            ICounterPayload payload = new UpDownCounterPayload(counterInfo, "DisplayName", "", null, Value1, Timestamp);

            MemoryStream stream = await GetMetrics(new() { payload });

            List<string> lines = ReadStream(stream);

            string metricName = $"{MeterName.ToLowerInvariant()}_{payload.CounterMetadata.CounterName}";

            Assert.Equal(3, lines.Count);
            Assert.Equal(FormattableString.Invariant($"# HELP {metricName}{payload.Unit} {payload.DisplayName}"), lines[0]);
            Assert.Equal(FormattableString.Invariant($"# TYPE {metricName} gauge"), lines[1]);
            Assert.Equal(FormattableString.Invariant($"{metricName} {payload.Value} {new DateTimeOffset(payload.Timestamp).ToUnixTimeMilliseconds()}"), lines[2]);
        }

        [Fact]
        public async Task UpDownCounterFormat_Test_Tags()
        {
            string meterTags = "MeterTagKey=MeterTagValue,MeterTagKey2=MeterTagValue2";
            string instrumentTags = "InstrumentTagKey=InstrumentTagValue,InstrumentTagKey2=InstrumentTagValue2";
            string scopeHash = "123";

            CounterMetadata counterInfo = new CounterMetadata(MeterName, InstrumentName, meterTags, instrumentTags, scopeHash);

            ICounterPayload payload = new UpDownCounterPayload(counterInfo, "DisplayName", "", null, Value1, Timestamp);

            MemoryStream stream = await GetMetrics(new() { payload });

            List<string> lines = ReadStream(stream);

            string metricName = $"{MeterName.ToLowerInvariant()}_{payload.CounterMetadata.CounterName}";
            string metricTags = "{MeterTagKey=\"MeterTagValue\", MeterTagKey2=\"MeterTagValue2\", InstrumentTagKey=\"InstrumentTagValue\", InstrumentTagKey2=\"InstrumentTagValue2\"}";

            Assert.Equal(3, lines.Count);
            Assert.Equal(FormattableString.Invariant($"# HELP {metricName}{payload.Unit} {payload.DisplayName}"), lines[0]);
            Assert.Equal(FormattableString.Invariant($"# TYPE {metricName} gauge"), lines[1]);
            Assert.Equal(FormattableString.Invariant($"{metricName}{metricTags} {payload.Value} {new DateTimeOffset(payload.Timestamp).ToUnixTimeMilliseconds()}"), lines[2]);
        }

        private async Task<MemoryStream> GetMetrics(List<ICounterPayload> payloads)
        {
            IMetricsStore metricsStore = new MetricsStore(_logger, MetricCount);

            foreach (var payload in payloads)
            {
                metricsStore.AddMetric(payload);
            }

            var outputStream = new MemoryStream();
            await metricsStore.SnapshotMetrics(outputStream, CancellationToken.None);

            return outputStream;
        }

        private static List<string> ReadStream(Stream stream)
        {
            var lines = new List<string>();

            stream.Position = 0;
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    lines.Add(reader.ReadLine());
                }
            }

            return lines;
        }
    }
}
