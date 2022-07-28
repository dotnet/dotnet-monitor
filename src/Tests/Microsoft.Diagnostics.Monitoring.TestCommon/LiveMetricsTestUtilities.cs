// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal class LiveMetricsTestUtilities
    {
        internal static async Task ValidateMetrics(IEnumerable<string> expectedProviders, IEnumerable<string> expectedNames,
            IAsyncEnumerable<CounterPayload> actualMetrics, bool strict)
        {
            HashSet<string> actualProviders = new();
            HashSet<string> actualNames = new();

            await AggregateMetrics(actualMetrics, actualProviders, actualNames);

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

        private static async Task AggregateMetrics(IAsyncEnumerable<CounterPayload> actualMetrics,
            HashSet<string> providers,
            HashSet<string> names)
        {
            await foreach (CounterPayload counter in actualMetrics)
            {
                providers.Add(counter.Provider);
                names.Add(counter.Name);
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
