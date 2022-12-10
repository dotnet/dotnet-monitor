// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal sealed class OperationTrackerService
    {
        private readonly ConcurrentDictionary<IEndpointInfo, OperationsTracker> _operations = new();

        private sealed class OperationsTracker : IDisposable
        {
            private int _count;

            public IDisposable Register()
            {
                Interlocked.Increment(ref _count);
                return this;
            }

            public bool IsExecutingOperation => 0 != Interlocked.CompareExchange(ref _count, 0, 0);

            public void Dispose() => Interlocked.Decrement(ref _count);
        }

        public bool IsExecutingOperation(IEndpointInfo endpointInfo)
        {
            return GetOperationsTracker(endpointInfo).IsExecutingOperation;
        }

        public IDisposable Register(IEndpointInfo endpointInfo)
        {
            return GetOperationsTracker(endpointInfo).Register();
        }

        public void EndpointRemoved(IEndpointInfo endpointInfo)
        {
            _operations.TryRemove(endpointInfo, out _);
        }

        private OperationsTracker GetOperationsTracker(IEndpointInfo endpointInfo)
        {
            return _operations.GetOrAdd(endpointInfo, _ => new OperationsTracker());
        }
    }
}
