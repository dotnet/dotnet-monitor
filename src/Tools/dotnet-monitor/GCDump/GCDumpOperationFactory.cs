// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class GCDumpOperationFactory : IGCDumpOperationFactory
    {
        private readonly OperationTrackerService _trackerService;
        private readonly ILogger<GCDumpOperation> _logger;

        public GCDumpOperationFactory(OperationTrackerService trackerService, ILogger<GCDumpOperation> logger)
        {
            _trackerService = trackerService;
            _logger = logger;
        }

        public IArtifactOperation Create(IEndpointInfo endpointInfo)
        {
            return new GCDumpOperation(endpointInfo, _trackerService, _logger);
        }
    }
}
