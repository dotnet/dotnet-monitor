// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if STARTUPHOOK
using Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher.Models;
#else
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
#endif
using System;
using System.Threading;

namespace Microsoft.Diagnostics.Tools.Monitor.Profiler
{
    internal sealed class EmptyPayload { }

    internal sealed class StartCapturingParametersPayload
    {
        public Guid RequestId { get; set; } = Guid.Empty;
        public TimeSpan Duration { get; set; } = Timeout.InfiniteTimeSpan;
        public CaptureParametersConfiguration Configuration { get; set; } = new();
    }

    internal sealed class StopCapturingParametersPayload
    {
        public Guid RequestId { get; set; } = Guid.Empty;
    }
}
