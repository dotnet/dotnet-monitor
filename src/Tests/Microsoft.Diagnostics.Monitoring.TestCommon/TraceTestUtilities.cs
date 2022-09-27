// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tracing;
using Microsoft.FileFormats;
using Microsoft.FileFormats.ELF;
using Microsoft.FileFormats.MachO;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class TraceTestUtilities
    {
        public static async Task ValidateTrace(Stream traceStream)
        {
            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            using var eventSource = new EventPipeEventSource(traceStream);

            // Dispose event source when cancelled.
            using var _ = cancellationTokenSource.Token.Register(() => eventSource.Dispose());

            bool foundTraceObject = false;

            eventSource.Dynamic.All += (TraceEvent obj) =>
            {
                foundTraceObject = true;
            };

            await Task.Run(() => Assert.True(eventSource.Process()), cancellationTokenSource.Token);

            Assert.True(foundTraceObject);
        }
    }
}
