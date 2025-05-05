// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline
{
    internal sealed class ExceptionPipeline :
        IDisposable
    {
        private readonly ExceptionPipelineDelegate _exceptionHandler;
        private readonly ExceptionSourceBase _exceptionSource;

        private long _disposedState;

        private long _processingState;
        private const long WriteFlag = unchecked((long)0x8000000000000000);

        public ExceptionPipeline(ExceptionSourceBase exceptionSource, Action<ExceptionPipelineBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(exceptionSource);
            ArgumentNullException.ThrowIfNull(configure);

            _exceptionSource = exceptionSource;

            ExceptionPipelineBuilder builder = new();
            configure(builder);
            _exceptionHandler = builder.Build();
        }

        public void Start()
        {
            DisposableHelper.ThrowIfDisposed<ExceptionPipeline>(ref _disposedState);
            _processingState = 0;

            _exceptionSource.ExceptionAvailable += ExceptionSource_ExceptionAvailable;
        }

        public void Stop()
        {
            // We must do this first to prevent any new exception callbacks.
            _exceptionSource.ExceptionAvailable -= ExceptionSource_ExceptionAvailable;

            // Wait until all outstanding exception callbacks are finished. Once _processingState reaches 0, we will set the WriteFlag.
            // This will prevent any further exception handlers from processing.
            while (0 != Interlocked.CompareExchange(ref _processingState, WriteFlag, 0) && !DisposableHelper.IsDisposed(ref _disposedState))
            {
                // Wait for all exceptions to be processed
                Thread.Sleep(100);
            }
        }

        private void ExceptionSource_ExceptionAvailable(object? sender, ExceptionAvailableEventArgs args)
        {
            // DESIGN: While async patterns are typically favored over synchronous patterns,
            // this is intentionally synchronous. Use cases for making this asynchronous typically
            // involve I/O operations, however those can be dispatched to other threads if necessary
            // (e.g. EventSource provides events but diagnostic pipe events are queued and asynchronously emitted).
            // Synchronous execution is required for scenarios where the exception needs to be held
            // at the site of where it is thrown before allowing it to unwind (e.g. capturing a dump of the exception).

            SpinWait spinWait = new();

            while (true)
            {
                long state = Interlocked.Read(ref _processingState);
                if ((state & WriteFlag) == WriteFlag)
                {
                    // Stop has been requested but the event handler has already occurred. Return early.
                    return;
                }
                if (DisposableHelper.IsDisposed(ref _disposedState))
                {
                    return;
                }
                // Increment the value by 1 to indicate that we are doing work.
                // We cannot do a simple increment since it's possible for a Stop call to attempt to set the write bit
                // at this stage.
                if (Interlocked.CompareExchange(ref _processingState, state + 1, state) == state)
                {
                    break;
                }
                spinWait.SpinOnce();
            }

            try
            {
                _exceptionHandler.Invoke(
                    args.Exception,
                    new ExceptionPipelineExceptionContext(
                        args.Timestamp,
                        args.ActivityId,
                        args.ActivityIdFormat));

            }
            finally
            {
                Interlocked.Decrement(ref _processingState);
            }
        }


        public void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
                return;

            _exceptionSource.ExceptionAvailable -= ExceptionSource_ExceptionAvailable;
        }
    }
}
