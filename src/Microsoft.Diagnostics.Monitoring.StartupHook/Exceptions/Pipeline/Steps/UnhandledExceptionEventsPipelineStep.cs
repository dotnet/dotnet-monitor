// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Eventing;
using System;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline.Steps
{
    internal sealed class UnhandledExceptionEventsPipelineStep
    {
        private readonly ExceptionsEventSource _eventSource;
        private readonly ExceptionIdSource _idSource;
        private readonly ExceptionPipelineDelegate _next;

        public UnhandledExceptionEventsPipelineStep(ExceptionPipelineDelegate next, ExceptionsEventSource eventSource, ExceptionIdSource idSource)
        {
            ArgumentNullException.ThrowIfNull(next);
            ArgumentNullException.ThrowIfNull(eventSource);
            ArgumentNullException.ThrowIfNull(idSource);

            _eventSource = eventSource;
            _idSource = idSource;
            _next = next;
        }

        public void Invoke(Exception exception, ExceptionPipelineExceptionContext context)
        {
            ArgumentNullException.ThrowIfNull(exception);

            // Do not send via the EventSource unless a listener is active.
            if (_eventSource.IsEnabled())
            {
                _eventSource.ExceptionInstanceUnhandled(_idSource.GetId(exception));
            }

            _next(exception, context);
        }
    }
}
