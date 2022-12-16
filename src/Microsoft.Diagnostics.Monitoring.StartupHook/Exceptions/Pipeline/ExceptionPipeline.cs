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
        private readonly IExceptionSource _exceptionSource;

        private long _disposedState;

        public ExceptionPipeline(IExceptionSource exceptionSource, Action<ExceptionPipelineBuilder> configure)
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
            _exceptionSource.ExceptionThrown += ExceptionSource_ExceptionThrown;
        }

        private void ExceptionSource_ExceptionThrown(object? sender, Exception e)
        {
            _exceptionHandler.Invoke(e);
        }

        public void Dispose()
        {
            if (0 != Interlocked.CompareExchange(ref _disposedState, 1, 0))
                return;

            _exceptionSource.ExceptionThrown -= ExceptionSource_ExceptionThrown;
        }
    }
}
