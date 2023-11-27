// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Eventing;
using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline;
using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline.Steps;
using System;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions
{
    internal sealed class CurrentAppDomainExceptionProcessor : IDisposable
    {
        private readonly ExceptionsEventSource _eventSource = new();
        private readonly ExceptionIdSource _idSource = new();

        private readonly CurrentAppDomainFirstChanceExceptionSource _firstChanceSource;
        private readonly ExceptionPipeline _firstChancePipeline;

        private readonly CurrentAppDomainUnhandledExceptionSource _unhandledSource;
        private readonly ExceptionPipeline _unhandledPipeline;

        private readonly bool _includeMonitorExceptions;

        public CurrentAppDomainExceptionProcessor(bool includeMonitorExceptions)
        {
            _includeMonitorExceptions = includeMonitorExceptions;

            _firstChanceSource = new();
            _firstChancePipeline = new(_firstChanceSource, ConfigureFirstChancePipeline);

            _unhandledSource = new();
            _unhandledPipeline = new(_unhandledSource, ConfigureUnhandledPipeline);
        }

        public void Start()
        {
            _firstChancePipeline.Start();
            _unhandledPipeline.Start();
        }

        private void ConfigureFirstChancePipeline(ExceptionPipelineBuilder builder)
        {
            // Filtering out dotnet-monitor in-proc feature exceptions
            if (!_includeMonitorExceptions)
            {
                builder.Add(next => new FilterMonitorExceptionPipelineStep(next).Invoke);
            }
            // Process current exception and its inner exceptions
            builder.Add(next => new ExceptionDemultiplexerPipelineStep(next).Invoke);
            // Prevent rethrows from being evaluated; only care about origination of exceptions.
            builder.Add(next => new FilterRepeatExceptionPipelineStep(next).Invoke);
            // Report exception through event source
            builder.Add(next => new ExceptionEventsPipelineStep(next, _eventSource, _idSource).Invoke);
        }

        private void ConfigureUnhandledPipeline(ExceptionPipelineBuilder builder)
        {
            // Report exception through event source
            builder.Add(next => new UnhandledExceptionEventsPipelineStep(next, _eventSource, _idSource).Invoke);
        }

        public void Dispose()
        {
            _firstChancePipeline.Dispose();
            _firstChanceSource.Dispose();

            _unhandledPipeline.Dispose();
            _unhandledSource.Dispose();

            _eventSource.Dispose();
        }
    }
}
