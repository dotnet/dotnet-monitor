﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Represents the execution of an egress operation as well as its associated cancellation
    /// and operation id. This is mainly used to transfer data between controllers and the background egress service.
    /// </summary>
    internal sealed class EgressRequest : IDisposable
    {
        private bool _disposed;
        private IDisposable _limitTracker;

        public EgressRequest(Guid operationId, IEgressOperation egressOperation, IDisposable limitTracker)
        {
            OperationId = operationId;
            EgressOperation = egressOperation;
            _limitTracker = limitTracker;
        }

        public CancellationTokenSource CancellationTokenSource { get; } = new();

        public Guid OperationId { get; }

        public IEgressOperation EgressOperation { get; }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _limitTracker.Dispose();
                CancellationTokenSource.Dispose();
            }
        }
    }
}
