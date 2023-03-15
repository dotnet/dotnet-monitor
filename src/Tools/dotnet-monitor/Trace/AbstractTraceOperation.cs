// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;
using System;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal abstract class AbstractTraceOperation : PipelineArtifactOperation<EventTracePipeline>
    {
        // Buffer size matches FileStreamResult
        protected const int DefaultBufferSize = 0x10000;

        protected readonly EventTracePipelineSettings _settings;

        public AbstractTraceOperation(IEndpointInfo endpointInfo, EventTracePipelineSettings settings, OperationTrackerService trackerService, ILogger logger)
            : base(trackerService, logger, Utils.ArtifactType_Trace, endpointInfo, register: true)
        {
            _settings = settings;
        }

        public override string GenerateFileName()
        {
            return FormattableString.Invariant($"{Utils.GetFileNameTimeStampUtcNow()}_{EndpointInfo.ProcessId}.nettrace");
        }

        public override string ContentType => ContentTypes.ApplicationOctetStream;
    }
}
