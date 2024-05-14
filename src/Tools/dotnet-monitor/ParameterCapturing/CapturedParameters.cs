// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.ParameterCapturing;
using System;
using System.Collections.Generic;
using System.Diagnostics;

#nullable enable

namespace Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing
{
    internal sealed class CapturedParameters : ICapturedParameters
    {
        private readonly List<ParameterInfo> _parameters = [];

        public CapturedParameters(string? activityId, ActivityIdFormat activityIdFormat, int threadId, DateTime capturedDateTime, string methodName, string methodTypeName, string methodModuleName)
        {
            ActivityId = activityId;
            ActivityIdFormat = activityIdFormat;
            ThreadId = threadId;
            MethodName = methodName;
            TypeName = methodTypeName;
            ModuleName = methodModuleName;
            CapturedDateTime = capturedDateTime;
        }

        public void AddParameter(ParameterInfo parameter)
        {
            _parameters.Add(parameter);
        }

        public string? ActivityId { get; }

        public ActivityIdFormat ActivityIdFormat { get; }

        public int ThreadId { get; }

        public string ModuleName { get; }

        public string TypeName { get; }

        public string MethodName { get; }

        public IReadOnlyList<ParameterInfo> Parameters => _parameters;

        public DateTime CapturedDateTime { get; }
    }
}
