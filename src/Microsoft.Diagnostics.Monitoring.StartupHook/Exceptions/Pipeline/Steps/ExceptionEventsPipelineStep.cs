// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Eventing;
using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Identification;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline.Steps
{
    internal sealed class ExceptionEventsPipelineStep
    {
        private static readonly object ExceptionIdKey = new object();

        private readonly ExceptionsEventSource _eventSource = new();
        private readonly ExceptionGroupIdentifierCache _identifierCache;
        private readonly ExceptionPipelineDelegate _next;

        private ulong _nextExceptionId = 1;

        public ExceptionEventsPipelineStep(ExceptionPipelineDelegate next)
        {
            ArgumentNullException.ThrowIfNull(next);

            List<ExceptionGroupIdentifierCacheCallback> callbacks = new(1)
            {
                new ExceptionsEventSourceIdentifierCacheCallback(_eventSource)
            };

            _identifierCache = new ExceptionGroupIdentifierCache(callbacks);
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
                ReadOnlySpan<StackFrame> stackFrames = ComputeEffectiveCallStack(exception);

                ulong groupId = _identifierCache.GetOrAdd(new ExceptionGroupIdentifier(exception));

                ulong[] frameIds = _identifierCache.GetOrAdd(stackFrames);

                _eventSource.ExceptionInstance(
                    GetExceptionId(exception),
                    groupId,
                    exception.Message,
                    frameIds,
                    context.Timestamp,
                    GetExceptionId(exception.InnerException),
                    GetInnerExceptionsIds(exception)
                    );
            }

            _next(exception, context);
        }

        private static ReadOnlySpan<StackFrame> ComputeEffectiveCallStack(Exception exception)
        {
            // The stack trace of thrown exceptions is populated as the exception unwinds the
            // stack. In the case of observing the exception from the FirstChanceException event,
            // there is only one frame on the stack (the throwing frame). In order to get the
            // full call stack of the exception, get the current call stack of the thread and
            // filter out the call frames that are "above" the exceptions's throwing frame.
            StackFrame? throwingFrame = null;
            StackTrace exceptionStackTrace = new(exception, fNeedFileInfo: false);
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
            ReadOnlySpan<StackFrame> threadStackFrames = threadStackTrace.GetFrames();
            int index = 0;
            while (index < threadStackFrames.Length)
            {
                StackFrame threadStackFrame = threadStackFrames[index];
                if (throwingFrame.GetMethod() == threadStackFrame.GetMethod() &&
                    throwingFrame.GetILOffset() == threadStackFrame.GetILOffset())
                {
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

        private ulong GetExceptionId(Exception? exception)
        {
            if (null == exception)
                return 0;

            ulong? exceptionId = null;
            if (exception.Data.Contains(ExceptionIdKey))
            {
                exceptionId = (ulong?)exception.Data[ExceptionIdKey];
            }

            if (!exceptionId.HasValue)
            {
                lock (exception.Data)
                {
                    if (!exceptionId.HasValue)
                    {
                        exceptionId = Interlocked.Increment(ref _nextExceptionId);

                        exception.Data[ExceptionIdKey] = exceptionId;
                    }
                }
            }

            return exceptionId.Value;
        }

        private ulong[] GetInnerExceptionsIds(Exception exception)
        {
            if (exception is AggregateException aggregateException)
            {
                ulong[] exceptionIds = new ulong[aggregateException.InnerExceptions.Count];
                for (int i = 0; i < aggregateException.InnerExceptions.Count; i++)
                {
                    exceptionIds[i] = GetExceptionId(aggregateException.InnerExceptions[i]);
                }
                return exceptionIds;
            }
            return Array.Empty<ulong>();
        }
    }
}
