// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class MetricsOperationFactory : IMetricsOperationFactory
    {
        private readonly ILogger<MetricsOperation> _logger;

        public MetricsOperationFactory(ILogger<MetricsOperation> logger)
        {
            _logger = logger;
        }

        public IArtifactOperation Create(IEndpointInfo endpointInfo, EventPipeCounterPipelineSettings settings)
        {
            return new MetricsOperation(endpointInfo, settings, _logger);
        }
    }
}
