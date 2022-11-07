// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class TraceOperationFactory : ITraceOperationFactory
    {
        private readonly ILogger<TraceOperation> _logger;

        public TraceOperationFactory(ILogger<TraceOperation> logger)
        {
            _logger = logger;
        }

        public IArtifactOperation Create(IEndpointInfo endpointInfo, EventTracePipelineSettings settings, LogFormat format)
        {
            if (true)
            {
                return new TraceUntilEventOperation(endpointInfo, settings, format, _logger);
            }
            else
            {
                return new TraceOperation(endpointInfo, settings, format, _logger);
            }
        }
    }
}
