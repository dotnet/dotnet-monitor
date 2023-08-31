// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions
{
    /// <summary>
    /// Represents a source of exceptions.
    /// </summary>
    internal abstract class ExceptionSourceBase
    {
        protected void RaiseException(Exception ex, DateTime timestamp, string? activityId, ActivityIdFormat format)
        {
            ExceptionAvailable?.Invoke(this, new ExceptionAvailableEventArgs(ex, timestamp, activityId, format));
        }

        /// <summary>
        /// Event that is raised each time an exception is made available.
        /// </summary>
        public event EventHandler<ExceptionAvailableEventArgs>? ExceptionAvailable;
    }
}
