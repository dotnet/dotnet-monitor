// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal static class LiveMetricsTestUtilities
    {
        internal static async Task ValidateMetrics(IEnumerable<string> expectedProviders, IEnumerable<string> expectedNames,
            IAsyncEnumerable<CounterPayload> actualMetrics, bool strict)
        {
            List<string> actualProviders = new();
            List<string> actualNames = new();
            List<string> actualMetadata = new();

            await AggregateMetrics(actualMetrics, actualProviders, actualNames, actualMetadata);

            ValidateMetrics(expectedProviders, expectedNames, actualProviders.ToHashSet(), actualNames.ToHashSet(), strict);
        }

        internal static void ValidateMetrics(IEnumerable<string> expectedProviders, IEnumerable<string> expectedNames,
            HashSet<string> actualProviders, HashSet<string> actualNames, bool strict)
        {
            CompareSets(new HashSet<string>(expectedProviders), actualProviders, strict);
            CompareSets(new HashSet<string>(expectedNames), actualNames, strict);
        }

        private static void CompareSets(HashSet<string> expected, HashSet<string> actual, bool strict)
        {
            bool matched = true;
            if (strict && !expected.SetEquals(actual))
            {
                expected.SymmetricExceptWith(actual);
                matched = false;
            }
            else if (!strict && !expected.IsSubsetOf(actual))
            {
                //actual must contain at least the elements in expected, but can contain more
                expected.ExceptWith(actual);
                matched = false;
            }
            Assert.True(matched, "Missing or unexpected elements: " + string.Join(",", expected));
        }

        internal static async Task AggregateMetrics(IAsyncEnumerable<CounterPayload> actualMetrics,
            List<string> providers,
            List<string> names,
            List<string> metadata)
        {
            await foreach (CounterPayload counter in actualMetrics)
            {
                providers.Add(counter.Provider);
                names.Add(counter.Name);
                metadata.Add(counter.Metadata);
            }
        }

        internal static async Task AggregateMetrics(IAsyncEnumerable<CounterPayload> actualMetrics,
            List<string> providers,
            List<string> names,
            List<string> metadata,
            List<string> meterTags,
            List<string> instrumentTags)
        {
            await foreach (CounterPayload counter in actualMetrics)
            {
                providers.Add(counter.Provider);
                names.Add(counter.Name);
                metadata.Add(counter.Metadata);
                meterTags.Add(counter.MeterTags);
                instrumentTags.Add(counter.InstrumentTags);
            }
        }

        internal static async IAsyncEnumerable<CounterPayload> GetAllMetrics(Stream liveMetricsStream)
        {
            using var reader = new StreamReader(liveMetricsStream);

            string entry = string.Empty;
            while ((entry = await reader.ReadLineAsync()) != null)
            {
                Assert.Equal(StreamingLogger.JsonSequenceRecordSeparator, (byte)entry[0]);
                yield return JsonSerializer.Deserialize<CounterPayload>(entry.Substring(1));
            }
        }
    }
}
