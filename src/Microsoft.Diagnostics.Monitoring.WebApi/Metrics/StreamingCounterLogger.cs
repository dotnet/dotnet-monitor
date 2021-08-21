// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal abstract class StreamingCounterLogger : ICountersLogger
    {
        private readonly Stream _stream;

        protected abstract void SerializeCounter(Stream stream, ICounterPayload counter);

        protected virtual void Cleanup() {}

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

        public void PipelineStarted()
        {
        }

        public void PipelineStopped()
        {
            Cleanup();
        }
    }
}
