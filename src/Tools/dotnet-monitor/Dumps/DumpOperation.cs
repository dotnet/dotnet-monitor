// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class DumpOperation : IArtifactOperation
    {
        private readonly IDumpService _dumpService;
        private readonly DumpType _dumpType;
        private readonly IEndpointInfo _endpointInfo;
        private readonly ILogger _logger;
        private readonly TaskCompletionSource _startCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public DumpOperation(IEndpointInfo endpointInfo, IDumpService dumpService, DumpType dumpType, ILogger logger)
        {
            _dumpService = dumpService;
            _dumpType = dumpType;
            _endpointInfo = endpointInfo;
            _logger = logger;
        }

        public string GenerateFileName()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                FormattableString.Invariant($"dump_{Utils.GetFileNameTimeStampUtcNow()}.dmp") :
                FormattableString.Invariant($"core_{Utils.GetFileNameTimeStampUtcNow()}");
        }

        public async Task ExecuteAsync(Stream outputStream, CancellationToken token)
        {
            _startCompletionSource.TrySetResult();

            using Stream dumpStream = await _dumpService.DumpAsync(_endpointInfo, _dumpType, token);

            await dumpStream.CopyToAsync(outputStream, token);
        }

        public Task StopAsync(CancellationToken token)
        {
            throw new MonitoringException(Strings.ErrorMessage_OperationIsNotStoppable);
        }

        public string ContentType => ContentTypes.ApplicationOctetStream;

        public bool IsStoppable => false;

        public Task Started => _startCompletionSource.Task;
    }
}
