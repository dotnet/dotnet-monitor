// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class TraceOperationFactory : ITraceOperationFactory
    {
        private readonly ILogger<TraceOperation> _logger;

        public TraceOperationFactory(ILogger<TraceOperation> logger)
        {
            _logger = logger;
        }

        public IArtifactOperation Create(IEndpointInfo endpointInfo, MonitoringSourceConfiguration configuration, TimeSpan duration, object stoppingEvent = null)
        {
            EventTracePipelineSettings settings = new()
            {
                Configuration = configuration,
                Duration = duration
            };

            // JSFIX: cast
            return stoppingEvent != null
                ? new TraceUntilEventOperation(endpointInfo, settings, (TraceEventFilter)stoppingEvent, _logger)
                : new TraceOperation(endpointInfo, settings, _logger);
        }
    }
}
