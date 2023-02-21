// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Eventing;
using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Identification;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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

            // Do not populate the cache or send via the EventSource until
            // a listener is active; otherwise, the listener will not receive the identifiers
            // for types/methods/modules/etc prior to listening to the event source. This
            // means that exceptions at startup are likely to be dropped if the listener was
            // not registered during the diagnostic startup suspension point.
            // CONSIDER: Possible improvement is to cache the information and then send off
            // the events once the listener is registered to effectively "catch up".
            if (_eventSource.IsEnabled())
            {
                ulong identifier = _identifierCache.GetOrAdd(new ExceptionIdentifier(exception));

                StackTrace stackTrace = new(exception, fNeedFileInfo: false);
                ulong[] frameIds = _identifierCache.GetOrAdd(stackTrace.GetFrames());

                _eventSource.ExceptionInstance(
                    identifier,
                    exception.Message,
                    frameIds);
            }

            _next(exception);
        }
    }
}
