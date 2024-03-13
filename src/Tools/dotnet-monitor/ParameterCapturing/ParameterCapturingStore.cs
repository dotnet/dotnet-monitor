// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.ParameterCapturing;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing
{
    internal sealed class ParameterCapturingStore : IParameterCapturingStore
    {
        private readonly List<ICapturedParameters> _parameters = [];
        public void AddCapturedParameters(ICapturedParameters capturedParameters)
        {
            _parameters.Add(capturedParameters);
        }

        public IReadOnlyList<ICapturedParameters> GetCapturedParameters() => _parameters.AsReadOnly();
    }
}
