// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal sealed class EgressOperationQueue : IEgressOperationQueue, IDisposable
    {
        private readonly Channel<EgressRequest> _queue;
        private bool _disposed;

        public EgressOperationQueue()
        {
            var options = new BoundedChannelOptions(256)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<EgressRequest>(options);
        }

        public ValueTask EnqueueAsync(
            EgressRequest workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            return _queue.Writer.WriteAsync(workItem);
        }

        public ValueTask<EgressRequest> DequeueAsync(
            CancellationToken cancellationToken)
        {
            return _queue.Reader.ReadAsync(cancellationToken);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _queue.Writer.Complete();
            }
        }
    }
}
