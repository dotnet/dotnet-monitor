// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline.Steps;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class ExceptionPipelineTests
    {
        private static readonly Action<ExceptionPipelineBuilder> EmptyConfigure = _ => { };


        [Fact]
        public void ExceptionPipeline_NoSteps()
        {
            using MockExceptionSource source = new();
            using ExceptionPipeline pipeline = new(source, EmptyConfigure);

            pipeline.Start();

            source.ProvideException(new Exception());
        }

        [Fact]
        public void ExceptionPipeline_NotStarted()
        {
            List<int> steps = new();

            using MockExceptionSource source = new();
            using ExceptionPipeline pipeline = new(
                source,
                builder => builder.Add(AppendIntegerPipelineStep.CreateBuilderStep(steps, 3)));

            source.ProvideException(new Exception());

            Assert.Empty(steps);
        }

        [Fact]
        public void ExceptionPipeline_SingleException()
        {
            const int ExpectedId = 3;

            List<int> steps = new(1);

            using MockExceptionSource source = new();
            using ExceptionPipeline pipeline = new(
                source,
                builder => builder.Add(AppendIntegerPipelineStep.CreateBuilderStep(steps, ExpectedId)));

            pipeline.Start();

            source.ProvideException(new Exception());

            int id = Assert.Single(steps);
            Assert.Equal(ExpectedId, id);
        }

        [Fact]
        public void ExceptionPipeline_RepeatedException()
        {
            const int ExpectedId = 5;
            const int ExpectedCount = 7;

            List<int> steps = new(ExpectedCount);

            using MockExceptionSource source = new();
            using ExceptionPipeline pipeline = new(
                source,
                builder => builder.Add(AppendIntegerPipelineStep.CreateBuilderStep(steps, ExpectedId)));

            pipeline.Start();

            // Simulates same exception rethrown several times
            Exception exception = new();
            for (int i = 0; i < ExpectedCount; i++)
            {
                source.ProvideException(exception);
            }

            Assert.Equal(ExpectedCount, steps.Count);
            Assert.All(steps, id => Assert.Equal(ExpectedId, id));
        }
    }
}
