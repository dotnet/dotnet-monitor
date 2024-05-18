// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class DumpOperation : IArtifactOperation
    {
        private readonly IDumpService _dumpService;
        private readonly DumpType _dumpType;
        private readonly IProcessInfo _processInfo;
        private readonly DumpFilePathTemplate _filePathTemplate;
        private readonly TaskCompletionSource _startCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public DumpOperation(IProcessInfo processInfo, IDumpService dumpService, DumpType dumpType, DumpFilePathTemplate filePathTemplate)
        {
            _dumpService = dumpService;
            _dumpType = dumpType;
            _processInfo = processInfo;
            _filePathTemplate = filePathTemplate;
        }

        public string GenerateFileName()
        {
            return _filePathTemplate.ToString(_processInfo);
        }

        public async Task ExecuteAsync(Stream outputStream, CancellationToken token)
        {
            _startCompletionSource.TrySetResult();

            using Stream dumpStream = await _dumpService.DumpAsync(_processInfo.EndpointInfo, _dumpType, token);

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
