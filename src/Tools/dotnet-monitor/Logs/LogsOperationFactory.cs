// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class LogsOperationFactory : ILogsOperationFactory
    {
        private readonly OperationTrackerService _operationTrackerService;
        private readonly ILogger<LogsOperation> _logger;

        public LogsOperationFactory(OperationTrackerService operationTrackerService, ILogger<LogsOperation> logger)
        {
            _operationTrackerService = operationTrackerService;
            _logger = logger;
        }

        public IArtifactOperation Create(IEndpointInfo endpointInfo, EventLogsPipelineSettings settings, LogFormat format)
        {
            return new LogsOperation(endpointInfo, settings, format, _operationTrackerService, _logger);
        }
    }
}
