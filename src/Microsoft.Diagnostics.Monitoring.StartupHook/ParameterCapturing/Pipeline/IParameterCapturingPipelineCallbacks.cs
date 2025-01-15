// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.FunctionProbes
{
    internal interface IParameterCapturingPipelineCallbacks
    {
        public void CapturingStart(StartCapturingParametersPayload request, IList<MethodInfo> methods);
        public void CapturingStop(Guid requestId);
        public void FailedToCapture(Guid requestId, ParameterCapturingEvents.CapturingFailedReason reason, string details);
        public void ProbeFault(Guid requestId, InstrumentedMethod faultingMethod);
    }
}
