// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class JsonCounterLoggerTests
    {
        private const string MeterName = "MeterName";
        private const string InstrumentName = "InstrumentName";
        private const int IntervalSeconds = 10;

        private static readonly DateTime Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(10000000).DateTime;

        [Theory]
        // Escaped input, then the decoded tag string the JSON output should expose.
        [InlineData(@"Key=Value", "Key=Value")]
        [InlineData(@"Key=a\,b", "Key=a,b")]
        [InlineData(@"Key=a\=b", "Key=a=b")]
        [InlineData(@"Path=C:\\temp", @"Path=C:\temp")]
        [InlineData(@"Key=a\\b\,c\=d", @"Key=a\b,c=d")]
        [InlineData(@"a\,b=v,c\=d=w", "a,b=v,c=d=w")]
        [InlineData(@"Key=", "Key=")]
        [InlineData(@"Flag", "Flag=")]
        [InlineData("", "")]
        public async Task ValueTags_AreDecoded(string valueTags, string expectedTags)
        {
            CounterMetadata counterInfo = new CounterMetadata(MeterName, InstrumentName, meterTags: null, instrumentTags: null, scopeHash: null);

            JsonElement counter = await SerializeAsync(new RatePayload(counterInfo, "DisplayName", string.Empty, valueTags, 1, IntervalSeconds, Timestamp));

            Assert.Equal(expectedTags, counter.GetProperty("tags").GetString());
        }

        [Fact]
        public async Task MeterAndInstrumentTags_AreDecoded()
        {
            CounterMetadata counterInfo = new CounterMetadata(
                MeterName,
                InstrumentName,
                meterTags: @"Meter\,Key=meter\=value",
                instrumentTags: @"Path=C:\\temp,Empty=",
                scopeHash: null);

            JsonElement counter = await SerializeAsync(new GaugePayload(counterInfo, "DisplayName", string.Empty, null, 1, Timestamp));

            Assert.Equal("Meter,Key=meter=value", counter.GetProperty("meterTags").GetString());
            Assert.Equal(@"Path=C:\temp,Empty=", counter.GetProperty("instrumentTags").GetString());
        }

        [Fact]
        public async Task Histogram_DecodesTagsAndKeepsPercentile()
        {
            CounterMetadata counterInfo = new CounterMetadata(MeterName, InstrumentName, meterTags: null, instrumentTags: null, scopeHash: null);

            JsonElement counter = await SerializeAsync(new AggregatePercentilePayload(
                counterInfo,
                "DisplayName",
                string.Empty,
                @"Route=/a\,b",
                new Quantile[] { new Quantile(0.5, 1) },
                Timestamp));

            Assert.Equal("Route=/a,b,Percentile=50", counter.GetProperty("tags").GetString());
        }

        [Fact]
        public async Task EventCounterMetadata_IsNotUnescaped()
        {
            // EventCounters metadata is unescaped and ':'-separated; a '\' in it is a literal.
            const string Metadata = @"Path:C:\temp,Key2:Value2";

            JsonElement counter = await SerializeAsync(new EventCounterPayload(
                Timestamp, "System.Runtime", "cpu-usage", "DisplayName", string.Empty, 1, CounterType.Metric, IntervalSeconds, IntervalSeconds, Metadata));

            Assert.Equal(Metadata, counter.GetProperty("tags").GetString());
        }

        [Fact]
        public async Task UntaggedCounter_KeepsNullTags()
        {
            CounterMetadata counterInfo = new CounterMetadata(MeterName, InstrumentName, meterTags: null, instrumentTags: null, scopeHash: null);

            JsonElement counter = await SerializeAsync(new GaugePayload(counterInfo, "DisplayName", string.Empty, null, 1, Timestamp));

            Assert.Equal(JsonValueKind.Null, counter.GetProperty("tags").ValueKind);
            Assert.Equal(JsonValueKind.Null, counter.GetProperty("meterTags").ValueKind);
            Assert.Equal(JsonValueKind.Null, counter.GetProperty("instrumentTags").ValueKind);
        }

        private static async Task<JsonElement> SerializeAsync(ICounterPayload payload)
        {
            using MemoryStream stream = new();

            JsonCounterLogger logger = new(stream, NullLogger.Instance);
            await logger.PipelineStarted(CancellationToken.None);
            logger.Log(payload);
            await logger.PipelineStopped(CancellationToken.None);

            // Each record is prefixed with the RS control character required by JSON sequences (RFC 7464).
            string content = System.Text.Encoding.UTF8.GetString(stream.ToArray()).Trim('\u001e', '\n');

            return JsonDocument.Parse(content).RootElement;
        }
    }
}
