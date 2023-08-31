// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions
{
    internal sealed class ExceptionAvailableEventArgs : EventArgs
    {
        public ExceptionAvailableEventArgs(Exception exception, DateTime timestamp, string? activityId, ActivityIdFormat activityIdFormat)
        {
            Exception = exception;
            Timestamp = timestamp;
            ActivityId = activityId;
            ActivityIdFormat = activityIdFormat;
        }

        public Exception Exception { get; }

        public DateTime Timestamp { get; }

        public string? ActivityId { get; }

        public ActivityIdFormat ActivityIdFormat { get; }
    }
}
