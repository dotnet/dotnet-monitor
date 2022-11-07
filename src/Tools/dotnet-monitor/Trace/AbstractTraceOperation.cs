// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal abstract class AbstractTraceOperation : IArtifactOperation
    {
        // Buffer size matches FileStreamResult
        protected const int DefaultBufferSize = 0x10000;

        protected readonly ILogger _logger;
        protected readonly IEndpointInfo _endpointInfo;
        protected readonly EventTracePipelineSettings _settings;

        public AbstractTraceOperation(IEndpointInfo endpointInfo, EventTracePipelineSettings settings, ILogger logger)
        {
            _logger = logger;
            _endpointInfo = endpointInfo;
            _settings = settings;
        }

        public abstract Task ExecuteAsync(Stream outputStream, TaskCompletionSource<object> startCompletionSource, CancellationToken token);

        public string GenerateFileName()
        {
            return FormattableString.Invariant($"{Utils.GetFileNameTimeStampUtcNow()}_{_endpointInfo.ProcessId}.nettrace");
        }

        public string ContentType => ContentTypes.ApplicationOctetStream;
    }
}
