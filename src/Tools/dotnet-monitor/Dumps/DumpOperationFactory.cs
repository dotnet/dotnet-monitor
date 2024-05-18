// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class DumpOperationFactory : IDumpOperationFactory
    {
        private readonly IDumpService _dumpService;
        private readonly IOptionsMonitor<StorageOptions> _options;

        public DumpOperationFactory(IDumpService dumpService, IOptionsMonitor<StorageOptions> options)
        {
            _dumpService = dumpService;
            _options = options;
        }

        public IArtifactOperation Create(IProcessInfo processInfo, DumpType dumpType)
        {
            StorageOptions options = _options.CurrentValue;
            return new DumpOperation(processInfo, _dumpService, dumpType, DumpFilePathTemplate.Parse(options.DumpFileNameTemplate));
        }
    }
}
