// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions
{
    internal sealed class MockExceptionSource :
        ExceptionSourceBase,
        IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }

        public void ProvideException(Exception exception)
        {
            RaiseException(exception, DateTime.UtcNow, Guid.Empty.ToString(), ActivityIdFormat.Unknown);
        }
    }
}
