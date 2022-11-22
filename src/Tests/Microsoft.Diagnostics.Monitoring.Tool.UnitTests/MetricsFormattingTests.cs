﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Amazon.Runtime;
using Amazon.S3.Model;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;
using Microsoft.FileFormats.PDB;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class MetricsFormattingTests
    {
        private ITestOutputHelper _outputHelper;


        private readonly string MeterName = "MeterName";
        private readonly string InstrumentName = "InstrumentName";
        private readonly DateTime Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(10000000).DateTime;
        private readonly ILogger<MetricsStoreService> _logger;

        private const int MetricCount = 1;
        private const int Value = 50;
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

            Dictionary<string, string> metadataDict1 = new();
            metadataDict1.Add("quantile", "0.5");
            payload.Add(new PercentilePayload(MeterName, InstrumentName, "DisplayName", "", metadataDict1, Value, Timestamp));

            Dictionary<string, string> metadataDict2 = new();
            metadataDict2.Add("quantile", "0.95");
            payload.Add(new PercentilePayload(MeterName, InstrumentName, "DisplayName", "", metadataDict2, Value, Timestamp));

            Dictionary<string, string> metadataDict3 = new();
            metadataDict3.Add("quantile", "0.99");
            payload.Add(new PercentilePayload(MeterName, InstrumentName, "DisplayName", "", metadataDict3, Value, Timestamp));

            MemoryStream stream = await GetMetrics(payload);

            List<string> lines = ReadStream(stream);

            // Question - this is manually recreating what PrometheusDataModel.GetPrometheusNormalizedName does to get the metric name;
            // should we call this method, or should this also be implicitly testing its behavior by having this hard-coded?
            string metricName = $"{MeterName.ToLowerInvariant()}_{payload[0].Name}";

            const string quantile_50 = "{quantile=\"0.5\"}";
            const string quantile_95 = "{quantile=\"0.95\"}";
            const string quantile_99 = "{quantile=\"0.99\"}";

            // This assumes the default quantiles of .5, .95, and .99
            Assert.Equal(5, lines.Count);
            Assert.Equal($"# HELP {metricName}{payload[0].Unit} {payload[0].DisplayName}", lines[0]);
            Assert.Equal($"# TYPE {metricName} summary", lines[1]);
            Assert.Equal($"{metricName}{quantile_50} {payload[0].Value}", lines[2]);
            Assert.Equal($"{metricName}{quantile_95} {payload[1].Value}", lines[3]);
            Assert.Equal($"{metricName}{quantile_99} {payload[2].Value}", lines[4]);

        }

        [Fact]
        public async Task GaugeFormat_Test()
        {
            ICounterPayload payload = new GaugePayload(MeterName, InstrumentName, "DisplayName", "", new(), Value, Timestamp);

            MemoryStream stream = await GetMetrics(new() { payload });

            List<string> lines = ReadStream(stream);

            // Question - this is manually recreating what PrometheusDataModel.GetPrometheusNormalizedName does to get the metric name;
            // should we call this method, or should this also be implicitly testing its behavior by having this hard-coded?
            string metricName = $"{MeterName.ToLowerInvariant()}_{payload.Name}";

            Assert.Equal(3, lines.Count);
            Assert.Equal($"# HELP {metricName}{payload.Unit} {payload.DisplayName}", lines[0]);
            Assert.Equal($"# TYPE {metricName} gauge", lines[1]);
            Assert.Equal($"{metricName} {payload.Value} {new DateTimeOffset(payload.Timestamp).ToUnixTimeMilliseconds()}", lines[2]);
        }

        [Fact]
        public async Task CounterFormat_Test()
        {
            ICounterPayload payload = new RatePayload(MeterName, InstrumentName, "DisplayName", "", new(), Value, IntervalSeconds, Timestamp);

            MemoryStream stream = await GetMetrics(new() { payload });

            List<string> lines = ReadStream(stream);

            // Question - this is manually recreating what PrometheusDataModel.GetPrometheusNormalizedName does to get the metric name;
            // should we call this method, or should this also be implicitly testing its behavior by having this hard-coded?
            string metricName = $"{MeterName.ToLowerInvariant()}_{payload.Name}";

            Assert.Equal(3, lines.Count);
            Assert.Equal($"# HELP {metricName}{payload.Unit} {payload.DisplayName}", lines[0]);
            Assert.Equal($"# TYPE {metricName} counter", lines[1]);
            Assert.Equal($"{metricName} {payload.Value} {new DateTimeOffset(payload.Timestamp).ToUnixTimeMilliseconds()}", lines[2]);
        }

        private async Task<MemoryStream> GetMetrics(List<ICounterPayload> payloads)
        {
            IMetricsStore metricsStore = new MetricsStore(_logger, MetricCount);
            metricsStore.AddMetric(payloads);

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

        /*
        [Fact]
        public async Task MaxHistogramFailure_Test()
        {

        }

        [Fact]
        public async Task MaxTimeSeriesFailure_Test()
        {

        }*/
    }
}
