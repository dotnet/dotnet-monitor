// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal abstract class StreamingCounterLogger : ICountersLogger
    {
        private const int CounterBacklog = 1000;

        // The amount of time to wait for serialization to finish before stopping the pipeline
        private static readonly TimeSpan FinishSerializationTimeout = TimeSpan.FromSeconds(3);

        private readonly Channel<ICounterPayload> _channel;
        private readonly ChannelReader<ICounterPayload> _channelReader;
        private readonly ChannelWriter<ICounterPayload> _channelWriter;
        private readonly ManualResetEvent _finishedSerialization = new(false);
        private readonly ILogger _logger;

        protected ILogger Logger => _logger;

        private long _dropCount;

        protected abstract Task SerializeAsync(ICounterPayload counter);

        protected StreamingCounterLogger(ILogger logger)
        {
            _channel = Channel.CreateBounded<ICounterPayload>(
                new BoundedChannelOptions(CounterBacklog)
                {
                    AllowSynchronousContinuations = false,
                    FullMode = BoundedChannelFullMode.DropWrite,
                    SingleReader = true,
                    SingleWriter = true
                },
                ChannelItemDropped);
            _channelReader = _channel.Reader;
            _channelWriter = _channel.Writer;
            _logger = logger;
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

            if (_dropCount > 0)
            {
                _logger.MetricsDropped(_dropCount);
            }

            if (!_finishedSerialization.WaitOne(FinishSerializationTimeout))
            {
                _logger.MetricsAbandonCompletion();
            }

            try
            {
                int pendingCount = _channelReader.Count;
                if (pendingCount > 0)
                {
                    _logger.MetricsUnprocessed(pendingCount);
                }
            }
            catch (Exception)
            {
            }
        }

        private void ChannelItemDropped(ICounterPayload payload)
        {
            _dropCount++;
        }

        private async Task ReadAndSerializeAsync()
        {
            try
            {
                while (await _channelReader.WaitToReadAsync())
                {
                    await SerializeAsync(await _channelReader.ReadAsync());
                }
            }
            catch (Exception ex)
            {
                _logger.MetricsWriteFailed(ex);
            }
            finally
            {
                _finishedSerialization.Set();
            }
        }
    }
}
