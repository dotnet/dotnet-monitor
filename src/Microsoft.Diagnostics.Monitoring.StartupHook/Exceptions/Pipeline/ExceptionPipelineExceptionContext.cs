// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline
{
    internal readonly ref struct ExceptionPipelineExceptionContext
    {
        public ExceptionPipelineExceptionContext(DateTime timestamp)
            : this(timestamp, null, ActivityIdFormat.Unknown)
        {
        }

        public ExceptionPipelineExceptionContext(DateTime timestamp, string? activityId, ActivityIdFormat activityIdFormat)
            : this(timestamp, activityId, activityIdFormat, isInnerException: false)
        {
        }

        public ExceptionPipelineExceptionContext(DateTime timestamp, string? activityId, ActivityIdFormat activityIdFormat, bool isInnerException)
        {
            Timestamp = timestamp;
            IsInnerException = isInnerException;
            ActivityId = activityId;
            ActivityIdFormat = activityIdFormat;
        }

        public bool IsInnerException { get; }

        public DateTime Timestamp { get; }

        public string? ActivityId { get; }

        public ActivityIdFormat ActivityIdFormat { get; }
    }
}
