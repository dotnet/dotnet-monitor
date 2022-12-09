// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class TraceTestUtilities
    {
        public static async Task ValidateTrace(Stream traceStream, bool? expectRundown = null)
        {
            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(CommonTestTimeouts.ValidateTraceTimeout);

            using var eventSource = new EventPipeEventSource(traceStream);

            // Dispose event source when cancelled.
            using var _ = cancellationTokenSource.Token.Register(eventSource.Dispose);

            bool foundTraceObject = false;
            bool foundRundown = false;

            eventSource.Dynamic.All += (TraceEvent obj) =>
            {
                foundTraceObject = true;
            };

            if (expectRundown.HasValue)
            {
                var rundown = new ClrRundownTraceEventParser(eventSource);
                rundown.RuntimeStart += (data) =>
                {
                    foundRundown = true;
                };
            }

            await Task.Run(() => Assert.True(eventSource.Process()), cancellationTokenSource.Token);

            Assert.True(foundTraceObject);

            if (expectRundown.HasValue)
            {
                Assert.Equal(expectRundown.Value, foundRundown);
            }
        }
    }
}
