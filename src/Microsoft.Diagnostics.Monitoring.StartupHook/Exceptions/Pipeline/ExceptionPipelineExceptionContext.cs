// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline
{
    internal readonly ref struct ExceptionPipelineExceptionContext
    {
        public ExceptionPipelineExceptionContext(DateTime timestamp)
            : this(timestamp, isInnerException: false)
        {
        }

        public ExceptionPipelineExceptionContext(DateTime timestamp, bool isInnerException)
        {
            Timestamp = timestamp;
            IsInnerException = isInnerException;
        }

        public bool IsInnerException { get; }

        public DateTime Timestamp { get; }
    }
}
