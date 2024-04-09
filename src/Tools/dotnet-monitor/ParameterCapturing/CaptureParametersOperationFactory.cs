// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Monitoring.WebApi.ParameterCapturing;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing
{
    internal sealed class CaptureParametersOperationFactory : ICaptureParametersOperationFactory
    {
        private readonly ProfilerChannel _profilerChannel;
        private readonly ILogger<CaptureParametersOperation> _logger;
        private readonly IParameterCapturingStore _parameterCapturingStore;

        public CaptureParametersOperationFactory(
            ProfilerChannel profilerChannel,
            ILogger<CaptureParametersOperation> logger,
            IParameterCapturingStore parameterCapturingStore)
        {
            _profilerChannel = profilerChannel;
            _logger = logger;
            _parameterCapturingStore = parameterCapturingStore;
        }

        public IArtifactOperation Create(IEndpointInfo endpointInfo, CaptureParametersConfiguration configuration, TimeSpan duration, CapturedParameterFormat format)
        {
            return new CaptureParametersOperation(endpointInfo, _profilerChannel, _logger, configuration, duration, format, _parameterCapturingStore);
        }
    }
}
