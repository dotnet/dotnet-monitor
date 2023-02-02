// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class DumpOperationFactory : IDumpOperationFactory
    {
        private readonly IDumpService _dumpService;
        private readonly ILogger<DumpOperation> _logger;

        public DumpOperationFactory(IDumpService dumpService, ILogger<DumpOperation> logger)
        {
            _dumpService = dumpService;
            _logger = logger;
        }

        public IArtifactOperation Create(IEndpointInfo endpointInfo, DumpType dumpType)
        {
            return new DumpOperation(endpointInfo, _dumpService, dumpType, _logger);
        }
    }
}
