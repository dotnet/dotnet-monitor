// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal abstract class StreamingCounterLogger : ICountersLogger
    {
        private const int BoundedChannelSize = 1000;

        // The amount of time to wait for serialization to finish before stopping the pipeline
        private static readonly TimeSpan FinishSerializationTimeout = TimeSpan.FromSeconds(3);

        private readonly Channel<ICounterPayload> _channel;
        private readonly ChannelWriter<ICounterPayload> _channelWriter;

        private ManualResetEvent _finishedSerialization = new(false);

        protected abstract Task SerializeAsync(ICounterPayload counter);

        protected StreamingCounterLogger()
        {
            _channel = Channel.CreateBounded<ICounterPayload>(
                new BoundedChannelOptions(BoundedChannelSize)
                {
                    AllowSynchronousContinuations = false,
                    FullMode = BoundedChannelFullMode.DropWrite,
                    SingleReader = true,
                    SingleWriter = true
                });
            _channelWriter = _channel.Writer;
        }

        public void Log(ICounterPayload counter)
        {
            _channelWriter.TryWrite(counter);
        }

        public void PipelineStarted()
        {
            _ = ReadAndSerializeAsync();
        }

        public void PipelineStopped()
        {
            _channelWriter.Complete();

            _finishedSerialization.WaitOne(FinishSerializationTimeout);
        }

        private async Task ReadAndSerializeAsync()
        {
            try
            {
                ChannelReader<ICounterPayload> reader = _channel.Reader;
                while (await reader.WaitToReadAsync())
                {
                    await SerializeAsync(await reader.ReadAsync());
                }
            }
            finally
            {
                _finishedSerialization.Set();
            }
        }
    }
}
