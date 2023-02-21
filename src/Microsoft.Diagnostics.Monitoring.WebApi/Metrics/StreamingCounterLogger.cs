﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal abstract class StreamingCounterLogger : ICountersLogger
    {
        private readonly Stream _stream;

        protected abstract void SerializeCounter(Stream stream, ICounterPayload counter);

        protected virtual void Cleanup() { }

        protected StreamingCounterLogger(Stream stream)
        {
            _stream = stream;
        }

        public void Log(ICounterPayload counter)
        {
            //CONSIDER
            //Ideally this would be an asynchronous api, but making this async would extend the lifetime of writing to the stream
            //beyond the lifetime of the Counters pipeline.

            SerializeCounter(_stream, counter);
        }

        public Task PipelineStarted(CancellationToken token) => Task.CompletedTask;

        public Task PipelineStopped(CancellationToken token)
        {
            Cleanup();
            return Task.CompletedTask;
        }
    }
}
