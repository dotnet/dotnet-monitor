// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline
{
    internal sealed class ExceptionPipelineExceptionContext
    {
        public ExceptionPipelineExceptionContext(DateTime timestamp)
        {
            Timestamp = timestamp;
        }

        public DateTime Timestamp { get; }
    }
}
