// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.ParameterCapturing;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing
{
    internal sealed class EventParameterCapturingPipelineCache
    {
        private readonly Dictionary<Guid, CapturedParameters> _capturedParameters = new();

        public bool TryStartNewCaptureResponse(Guid captureId, string activityId, ActivityIdFormat activityIdFormat, int threadId, DateTime capturedDateTime, string methodName, string methodTypeName, string methodModuleName)
        {
            return _capturedParameters.TryAdd(captureId, new CapturedParameters(activityId, activityIdFormat, threadId, capturedDateTime, methodName, methodTypeName, methodModuleName));
        }

        public bool TryGetCapturedParameters(Guid captureId, out ICapturedParameters capturedParameters)
        {
            if (_capturedParameters.TryGetValue(captureId, out CapturedParameters captured))
            {
                capturedParameters = captured;
                return true;
            }

            capturedParameters = null;
            return false;
        }

        public bool TryAddParameter(
            Guid captureId,
            string parameterName,
            string parameterType,
            string parameterTypeModuleName,
            string parameterValue,
            bool isInParameter,
            bool isOutParameter,
            bool isByRefParameter)
        {
            if (_capturedParameters.TryGetValue(captureId, out CapturedParameters captured))
            {
                captured.AddParameter(new ParameterInfo(
                    Name: parameterName,
                    Type: parameterType,
                    TypeModuleName: parameterTypeModuleName,
                    Value: parameterValue,
                    IsIn: isInParameter,
                    IsOut: isOutParameter,
                    IsByRef: isByRefParameter));
                return true;
            }
            return false;
        }
    }
}
