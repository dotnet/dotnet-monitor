// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.ParameterCapturing;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing
{
    internal sealed class CapturedParameters : ICapturedParameters
    {
        private List<ParameterInfo> _parameters = [];

        public CapturedParameters(Guid requestId, string activityId, DateTime capturedDateTime, string methodName, string methodTypeName, string methodModuleName)
        {
            RequestId = requestId;
            ActivityId = activityId;
            MethodName = methodName;
            TypeName = methodTypeName;
            ModuleName = methodModuleName;
            CapturedDateTime = capturedDateTime;
        }

        public void AddParameter(ParameterInfo parameter)
        {
            _parameters.Add(parameter);
        }

        public Guid RequestId { get; }

        public string ActivityId { get; }

        public string ModuleName { get; }

        public string TypeName { get; }

        public string MethodName { get; }

        public IReadOnlyList<ParameterInfo> Parameters => _parameters;

        public DateTime CapturedDateTime { get; }
    }
}
