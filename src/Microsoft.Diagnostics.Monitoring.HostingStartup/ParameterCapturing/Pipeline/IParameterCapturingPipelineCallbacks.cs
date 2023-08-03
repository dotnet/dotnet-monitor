// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes
{
    internal interface IParameterCapturingPipelineCallbacks
    {
        public void CapturingStart(StartCapturingParametersPayload request, List<MethodInfo> methods);
        public void CapturingStop(Guid RequestId);
        public void FailedToCapture(Guid RequestId, ParameterCapturingEvents.CapturingFailedReason Reason, string Details);
    }
}
