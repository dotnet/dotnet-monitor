// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions
{
    internal sealed class ExceptionEventArgs : EventArgs
    {
        public ExceptionEventArgs(Exception exception, DateTime timestamp)
        {
            Exception = exception;
            Timestamp = timestamp;
        }

        public Exception Exception { get; }

        public DateTime Timestamp { get; }
    }
}
