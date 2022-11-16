// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public MetricsFormattingTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        /*
        [Fact]
        public async Task HistogramFormat_Test()
        {
        }*/

        /*
        [Fact]
        public async Task GaugeFormat_Test()
        {

        }*/

        
        [Fact]
        public async Task CounterFormat_Test()
        {
            LoggerFactory factory = new LoggerFactory();
            var logger = factory.CreateLogger<MetricsStoreService>();

            IMetricsStore metricsStore = new MetricsStore(logger, 10); // 10 is arbitrary

            MemoryStream stream = new();
            CancellationTokenSource source = new(5000); // arbitrary

            const long milliseconds = 1;

            DateTime dt = new DateTime(1);

            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);

            ICounterPayload payload = new RatePayload("ProviderName", "CounterName", "DisplayName", "", new(), 40, 10, dateTimeOffset.DateTime);

            List<ICounterPayload> payloads = new()
            {
                payload
            };

            metricsStore.AddMetric(payloads);

            await metricsStore.SnapshotMetrics(stream, source.Token);

            List<string> lines = new();

            stream.Position = 0;
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    // StringStream creation and initialization
                    lines.Add(reader.ReadLine());
                }
            }

            // Question - this is manually recreating what PrometheusDataModel.GetPrometheusNormalizedName does to get the metric name;
            // should we call this method, or should this also be implicitly testing its behavior by having this hard-coded?
            string metricName = $"{payload.Provider.ToLowerInvariant()}_{payload.Name}";

            Assert.Equal(3, lines.Count);
            Assert.Equal($"# HELP {metricName}{payload.Unit} {payload.DisplayName}", lines[0]);
            Assert.Equal($"# TYPE {metricName} counter", lines[1]);
            Assert.Equal($"{metricName} {payload.Value} {dateTimeOffset.ToUnixTimeMilliseconds()}", lines[2]);
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
