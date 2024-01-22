// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline.Steps
{
    /// <summary>
    /// An exception pipeline step that will only allow an exception to be
    /// processed further if it was not caused by a dotnet-monitor in-proc feature.
    /// </summary>
    internal sealed class FilterMonitorExceptionPipelineStep
    {
        private readonly ExceptionPipelineDelegate _next;

        public FilterMonitorExceptionPipelineStep(ExceptionPipelineDelegate next)
        {
            ArgumentNullException.ThrowIfNull(next);

            _next = next;
        }

        public void Invoke(Exception exception, ExceptionPipelineExceptionContext context)
        {
            ArgumentNullException.ThrowIfNull(exception);

            if (MonitorExecutionContextTracker.IsInMonitorContext())
            {
                return;
            }

            _next(exception, context);
        }
    }
}
