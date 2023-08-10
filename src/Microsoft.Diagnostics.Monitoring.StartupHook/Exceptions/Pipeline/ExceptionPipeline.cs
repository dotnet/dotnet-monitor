// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline
{
    internal sealed class ExceptionPipeline :
        IDisposable
    {
        private readonly ExceptionPipelineDelegate _exceptionHandler;
        private readonly ExceptionSourceBase _exceptionSource;

        private long _disposedState;

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

            _exceptionSource.ExceptionAvailable += ExceptionSource_ExceptionAvailable;
        }

        private void ExceptionSource_ExceptionAvailable(object? sender, ExceptionAvailableEventArgs args)
        {
            // DESIGN: While async patterns are typically favored over synchronous patterns,
            // this is intentionally synchronous. Use cases for making this asynchronous typically
            // involve I/O operations, however those can be dispatched to other threads if necessary
            // (e.g. EventSource provides events but diagnostic pipe events are queued and asynchronously emitted).
            // Synchronous execution is required for scenarios where the exception needs to be held
            // at the site of where it is thrown before allowing it to unwind (e.g. capturing a dump of the exception).
            _exceptionHandler.Invoke(
                args.Exception,
                new ExceptionPipelineExceptionContext(
                    args.Timestamp,
                    args.ActivityId,
                    args.ActivityIdFormat));
        }


        public void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
                return;

            _exceptionSource.ExceptionAvailable -= ExceptionSource_ExceptionAvailable;
        }
    }
}
