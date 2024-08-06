// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

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

        private readonly Channel<ICounterPayload> _channel;
        private readonly ChannelReader<ICounterPayload> _channelReader;
        private readonly ChannelWriter<ICounterPayload> _channelWriter;
        private Task _processingTask;
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

        public Task PipelineStarted(CancellationToken token)
        {
            _processingTask = ReadAndSerializeAsync(token);
            return Task.CompletedTask;
        }

        public async Task PipelineStopped(CancellationToken token)
        {
            _channelWriter.Complete();

            if (_dropCount > 0)
            {
                _logger.MetricsDropped(_dropCount);
            }

            await _processingTask;

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

        private async Task ReadAndSerializeAsync(CancellationToken token)
        {
            try
            {
                while (await _channelReader.WaitToReadAsync(token))
                {
                    await SerializeAsync(await _channelReader.ReadAsync(token));
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.MetricsWriteFailed(ex);
            }
        }
    }
}
