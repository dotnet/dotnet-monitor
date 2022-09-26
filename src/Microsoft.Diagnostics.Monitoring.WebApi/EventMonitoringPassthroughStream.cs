﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tracing;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// A stream that can monitor an event stream which is compatible with <see cref="EventPipeEventSource"/>
    /// for a specific event while also passing along the event data to a destination stream.
    /// </summary>
    public class EventMonitoringPassthroughStream : Stream
    {
        private readonly Action<TraceEvent> _onEvent;
        private readonly string _providerName;
        private readonly string _eventName;
        private readonly Stream _sourceStream;
        private readonly Stream _destinationStream;

        private EventPipeEventSource _eventSource;

        // JSFIX: Add summary.
        // Key takeaway is that onEvent will only be invoked once, and the source stream will continue to transfer to
        // the destination stream even after the onEvent callback is invoked.
        public EventMonitoringPassthroughStream(
            string providerName,
            string eventName,
            Action<TraceEvent> onEvent,
            Stream sourceStream,
            Stream destinationStream,
            int bufferSize,
            bool leaveDestinationStreamOpen) : base()
        {
            _providerName = providerName;
            _eventName = eventName;
            _onEvent = onEvent;
            _sourceStream = sourceStream;

            // Wrap a buffered stream around the destination stream
            // to avoid slowing down the event processing with the data
            // passthrough unless there is significant pressure.
            _destinationStream = new BufferedStream(
                leaveDestinationStreamOpen
                    ? new StreamLeaveOpenWrapper(destinationStream)
                    : destinationStream,
                bufferSize);
        }

        /// <summary>
        /// Start processing the event stream, monitoring it for the requested event and transferring its data to the specified destination stream.
        /// This will continue to run until the event stream is complete or a stop is requested, regardless of if the requested event has been observed.
        /// </summary>
        /// <param name="token">The cancellation token. It can only be signaled before processing has been started. After that point <see cref="StopProcessing"/> or <see cref="DisposeAsync"/> should be called to stop processing.</param>
        /// <returns></returns>
        public Task ProcessAsync(CancellationToken token)
        {
            return Task.Run(() =>
            {
                _eventSource = new EventPipeEventSource(this);
                _eventSource.Dynamic.AddCallbackForProviderEvent(_providerName, _eventName, TraceEventCallback);

                // The EventPipeEventSource will drive the transferring of data to the destination stream as it processes events.
                _eventSource.Process();
            }, token);
        }

        public void StopProcessing()
        {
            _eventSource?.StopProcessing();
        }

        private void TraceEventCallback(TraceEvent obj)
        {
            // Once the specified event has been observed, stop watching for it.
            // However, keep processing the data as to allow remaining trace event
            // data, such as run down, to finish transferring to the destination stream.
            _eventSource.Dynamic.RemoveCallback<TraceEvent>(TraceEventCallback);

            _onEvent(obj);
        }

        public override bool CanSeek => false;
        public override bool CanWrite => false;

        public override bool CanTimeout => _sourceStream.CanRead;
        public override bool CanRead => _sourceStream.CanRead;
        public override long Length => _sourceStream.Length;

        public override long Position { get => _sourceStream.Position; set => _sourceStream.Position = value; }
        public override int ReadTimeout { get => _sourceStream.ReadTimeout; set => _sourceStream.ReadTimeout = value; }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void CopyTo(Stream destination, int bufferSize) => throw new NotSupportedException();
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => throw new NotSupportedException();

        public override void Flush() => _sourceStream.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => _sourceStream.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Read(buffer.AsSpan(offset, count));
        }

        public override int Read(Span<byte> buffer)
        {
            int bytesRead = _sourceStream.Read(buffer);
            if (bytesRead != 0)
            {
                _destinationStream.Write(buffer[..bytesRead]);
            }

            return bytesRead;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int bytesRead = await _sourceStream.ReadAsync(buffer, cancellationToken);
            if (bytesRead != 0)
            {
                await _destinationStream.WriteAsync(buffer[..bytesRead], cancellationToken);
            }

            return bytesRead;
        }

        public override async ValueTask DisposeAsync()
        {
            _eventSource?.Dispose();
            await _sourceStream.DisposeAsync();
            await _destinationStream.DisposeAsync();
            await base.DisposeAsync();
        }
    }
}
