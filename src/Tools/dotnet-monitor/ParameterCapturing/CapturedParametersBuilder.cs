// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Monitoring.WebApi.ParameterCapturing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing
{
    internal sealed class CapturedParametersBuilder
    {
        private readonly Dictionary<Guid, CapturedParameters> _capturedParameters = new();

        public bool TryStartNewCaptureResponse(Guid captureId, string? activityId, ActivityIdFormat activityIdFormat, int threadId, DateTime capturedDateTime, string methodName, string methodTypeName, string methodModuleName)
        {
            return _capturedParameters.TryAdd(captureId, new CapturedParameters(activityId, activityIdFormat, threadId, capturedDateTime, methodName, methodTypeName, methodModuleName));
        }

        public bool TryFinalizeParameters(Guid captureId, [NotNullWhen(true)] out ICapturedParameters? capturedParameters)
        {
            if (_capturedParameters.Remove(captureId, out CapturedParameters? captured))
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
            string? parameterValue,
            EvaluationFailureReason evalFailReason,
            bool isInParameter,
            bool isOutParameter,
            bool isByRefParameter)
        {
            if (_capturedParameters.TryGetValue(captureId, out CapturedParameters? captured))
            {
                captured.AddParameter(new ParameterInfo(
                    Name: parameterName,
                    Type: parameterType,
                    TypeModuleName: parameterTypeModuleName,
                    Value: parameterValue,
                    EvalFailReason: evalFailReason,
                    IsIn: isInParameter,
                    IsOut: isOutParameter,
                    IsByRef: isByRefParameter));
                return true;
            }
            return false;
        }
    }
}
