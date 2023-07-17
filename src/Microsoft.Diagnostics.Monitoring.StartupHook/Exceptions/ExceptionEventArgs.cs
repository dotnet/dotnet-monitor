// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Diagnostics;

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

        private Guid _activityId;

        private ActivityIdFormat _activityIdFormat;

        /// <summary>
        /// Gets the activity ID for the thread on which the event was written.
        /// </summary>
        public Guid ActivityId
        {
            get
            {
                if (_activityId == Guid.Empty)
                {
                    _activityId = EventSource.CurrentThreadActivityId;
                }

                return _activityId;
            }
        }

        public ActivityIdFormat ActivityIdFormat
        {
            get
            {
                _activityIdFormat = Activity.DefaultIdFormat;

                return _activityIdFormat;
            }
        }
    }
}
