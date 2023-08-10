// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Tools.Monitor.Stacks
{
    internal sealed class StacksOperationFactory : IStacksOperationFactory
    {
        private readonly ProfilerChannel _channel;
        private readonly OperationTrackerService _operationTrackerService;
        private readonly ILogger<StacksOperation> _logger;

        public StacksOperationFactory(ProfilerChannel channel, OperationTrackerService operationTrackerService, ILogger<StacksOperation> logger)
        {
            _channel = channel;
            _operationTrackerService = operationTrackerService;
            _logger = logger;
        }

        public IArtifactOperation Create(IEndpointInfo endpointInfo, StackFormat format)
        {
            return new StacksOperation(endpointInfo, format, _channel, _operationTrackerService, _logger);
        }
    }
}
