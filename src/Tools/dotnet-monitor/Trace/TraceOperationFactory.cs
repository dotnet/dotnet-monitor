// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class TraceOperationFactory : ITraceOperationFactory
    {
        private readonly ILogger<TraceOperation> _logger;

        public TraceOperationFactory(ILogger<TraceOperation> logger)
        {
            _logger = logger;
        }

        public IArtifactOperation Create(IEndpointInfo endpointInfo, MonitoringSourceConfiguration configuration, TimeSpan duration)
        {
            EventTracePipelineSettings settings = new()
            {
                Configuration = configuration,
                Duration = duration
            };

            return new TraceOperation(endpointInfo, settings, _logger);
        }

        public IArtifactOperation Create(IEndpointInfo endpointInfo, MonitoringSourceConfiguration configuration, TimeSpan duration, string providerName, string eventName, IDictionary<string, string> payloadFilter)
        {
            EventTracePipelineSettings settings = new()
            {
                Configuration = configuration,
                Duration = duration
            };

            return new TraceUntilEventOperation(endpointInfo, settings, providerName, eventName, payloadFilter, _logger);
        }
    }
}
