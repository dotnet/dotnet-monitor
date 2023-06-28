// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline;
using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline.Steps;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions
{
    internal sealed class CurrentAppDomainExceptionProcessor
    {
        private readonly ExceptionPipeline _pipeline;
        private readonly CurrentAppDomainExceptionSource _source;

        public CurrentAppDomainExceptionProcessor()
        {
            _source = new();
            _pipeline = new(_source, ConfigurePipeline);
        }

        public void Start()
        {
            _pipeline.Start();
        }

        private static void ConfigurePipeline(ExceptionPipelineBuilder builder)
        {
            // Process current exception and its inner exceptions
            builder.Add(next => new ExceptionDemultiplexerPipelineStep(next).Invoke);
            // Prevent rethrows from being evaluated; only care about origination of exceptions.
            builder.Add(next => new FilterRepeatExceptionPipelineStep(next).Invoke);
            // Report exception through event source
            builder.Add(next => new ExceptionEventsPipelineStep(next).Invoke);
        }
    }
}
