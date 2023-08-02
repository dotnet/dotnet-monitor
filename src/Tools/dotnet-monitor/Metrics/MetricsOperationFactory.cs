// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class MetricsOperationFactory : IMetricsOperationFactory
    {
        private readonly OperationTrackerService _operationTrackerService;
        private readonly ILogger<MetricsOperation> _logger;

        public MetricsOperationFactory(OperationTrackerService operationTrackerService, ILogger<MetricsOperation> logger)
        {
            _operationTrackerService = operationTrackerService;
            _logger = logger;
        }

        public IArtifactOperation Create(IEndpointInfo endpointInfo, MetricsPipelineSettings settings)
        {
            settings.UseSharedSession = endpointInfo.RuntimeVersion?.Major >= 8;
            return new MetricsOperation(endpointInfo, settings, _operationTrackerService, _logger);
        }
    }
}
