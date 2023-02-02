// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tracing;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class TraceEventExtensions
    {
        public static T GetPayload<T>(this TraceEvent traceEvent, int index)
        {
            return (T)traceEvent.PayloadValue(index);
        }
    }
}
