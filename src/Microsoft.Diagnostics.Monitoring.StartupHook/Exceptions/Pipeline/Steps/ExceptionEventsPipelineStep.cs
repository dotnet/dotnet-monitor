// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Eventing;
using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Identification;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline.Steps
{
    internal sealed class ExceptionEventsPipelineStep
    {
        private readonly ExceptionsEventSource _eventSource = new();
        private readonly ExceptionIdentifierCache _identifierCache;
        private readonly ExceptionPipelineDelegate _next;

        public ExceptionEventsPipelineStep(ExceptionPipelineDelegate next)
        {
            ArgumentNullException.ThrowIfNull(next);

            List<ExceptionIdentifierCacheCallback> callbacks = new(1)
            {
                new ExceptionsEventSourceIdentifierCacheCallback(_eventSource)
            };

            _identifierCache = new ExceptionIdentifierCache(callbacks);
            _next = next;
        }

        public void Invoke(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            ulong identifier = _identifierCache.GetOrAdd(new ExceptionIdentifier(exception));

            _eventSource.ExceptionInstance(identifier, exception.Message);

            _next(exception);
        }
    }
}
