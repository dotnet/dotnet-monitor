// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Eventing;
using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Identification;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline.Steps
{
    internal sealed class ExceptionEventsPipelineStep
    {
        private readonly ExceptionsEventSource _eventSource;
        private readonly ExceptionGroupIdentifierCache _identifierCache;
        private readonly ExceptionIdSource _idSource;
        private readonly ExceptionPipelineDelegate _next;

        public ExceptionEventsPipelineStep(ExceptionPipelineDelegate next, ExceptionsEventSource eventSource, ExceptionIdSource idSource)
        {
            ArgumentNullException.ThrowIfNull(next);
            ArgumentNullException.ThrowIfNull(eventSource);
            ArgumentNullException.ThrowIfNull(idSource);

            List<ExceptionGroupIdentifierCacheCallback> callbacks = new(1)
            {
                new ExceptionsEventSourceIdentifierCacheCallback(eventSource)
            };

            _eventSource = eventSource;
            _identifierCache = new ExceptionGroupIdentifierCache(callbacks);
            _idSource = idSource;
            _next = next;
        }

        public void Invoke(Exception exception, ExceptionPipelineExceptionContext context)
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
                ReadOnlySpan<StackFrame> stackFrames = ComputeEffectiveCallStack(exception, context.IsInnerException);

                ulong groupId = _identifierCache.GetOrAdd(new ExceptionGroupIdentifier(exception));

                ulong[] frameIds = _identifierCache.GetOrAdd(stackFrames);

                _eventSource.ExceptionInstance(
                    _idSource.GetId(exception),
                    groupId,
                    exception.Message,
                    frameIds,
                    context.Timestamp,
                    GetInnerExceptionsIds(exception),
                    context.ActivityId ?? string.Empty,
                    context.ActivityIdFormat
                    );
            }

            _next(exception, context);
        }

        private static ReadOnlySpan<StackFrame> ComputeEffectiveCallStack(Exception exception, bool isInnerException)
        {
            StackTrace exceptionStackTrace = new(exception, fNeedFileInfo: false);

            // Inner exceptions have complete call stacks because they were caught or do not
            // have any call stacks because they were never thrown. Report the stack frames as-is.
            if (isInnerException)
                return exceptionStackTrace.GetFrames();

            // The stack trace of thrown exceptions is populated as the exception unwinds the
            // stack. In the case of observing the exception from the FirstChanceException event,
            // there is only one frame on the stack (the throwing frame). In order to get the
            // full call stack of the exception, get the current call stack of the thread and
            // filter out the call frames that are "above" the exception's throwing frame.
            StackFrame? throwingFrame = null;
            foreach (StackFrame stackFrame in exceptionStackTrace.GetFrames())
            {
                if (null != stackFrame.GetMethod())
                {
                    throwingFrame = stackFrame;
                    break;
                }
            }

            if (null == throwingFrame)
            {
                return ReadOnlySpan<StackFrame>.Empty;
            }

            StackTrace threadStackTrace = new(fNeedFileInfo: false);
            Span<StackFrame> threadStackFrames = threadStackTrace.GetFrames();
            int index = 0;
            while (index < threadStackFrames.Length)
            {
                StackFrame threadStackFrame = threadStackFrames[index];

                // Workaround for https://github.com/dotnet/runtime/issues/91125
                // The threadStackFrame's ILOffset for eclipsing exceptions
                // thrown from within the catch block does not match the
                // ILOffset in the throwingFrame. Unconditionally swap out the
                // threadStackFrame for the matching throwingFrame to ensure
                // the correct ILOffset is used.
                if (throwingFrame.GetMethod() == threadStackFrame.GetMethod())
                {
                    threadStackFrames[index] = throwingFrame;
                    break;
                }

                index++;
            }

            if (index < threadStackTrace.FrameCount)
            {
                return threadStackFrames.Slice(index);
            }

            return ReadOnlySpan<StackFrame>.Empty;
        }

        private ulong[] GetInnerExceptionsIds(Exception exception)
        {
            if (exception is AggregateException aggregateException)
            {
                // AggregateException will always pull the first exception out of the list of inner exceptions
                // and use that as its InnerException property. No need to report the InnerException property value.
                ulong[] exceptionIds = new ulong[aggregateException.InnerExceptions.Count];
                for (int i = 0; i < aggregateException.InnerExceptions.Count; i++)
                {
                    exceptionIds[i] = _idSource.GetId(aggregateException.InnerExceptions[i]);
                }
                return exceptionIds;
            }
            else if (exception is ReflectionTypeLoadException reflectionTypeLoadException)
            {
                // ReflectionTypeLoadException does not set InnerException. No need to report the InnerException property value.
                ulong[] exceptionIds = new ulong[reflectionTypeLoadException.LoaderExceptions.Length];
                for (int i = 0; i < reflectionTypeLoadException.LoaderExceptions.Length; i++)
                {
                    Exception? loaderException = reflectionTypeLoadException.LoaderExceptions[i];
                    if (null != loaderException)
                    {
                        exceptionIds[i] = _idSource.GetId(loaderException);
                    }
                }
                return exceptionIds;
            }
            else if (null != exception.InnerException)
            {
                return new ulong[] { _idSource.GetId(exception.InnerException) };
            }
            return Array.Empty<ulong>();
        }
    }
}
