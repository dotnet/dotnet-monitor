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
    public sealed class ExceptionPipelineBuilderTests
    {
        [Fact]
        public void ExceptionPipelineBuilder_NoSteps()
        {
            ExceptionPipelineDelegate handler = new ExceptionPipelineBuilder()
                .Build();

            Assert.NotNull(handler);

            handler.Invoke(new Exception(), new ExceptionPipelineExceptionContext(DateTime.UtcNow));
        }

        [Fact]
        public void ExceptionPipelineBuilder_StepExecutionOrder()
        {
            const int Step1Id = 5;
            const int Step2Id = 8;
            const int Step3Id = 2;

            List<int> stepOrder = new List<int>();

            ExceptionPipelineDelegate handler = new ExceptionPipelineBuilder()
                .Add(AppendIntegerPipelineStep.CreateBuilderStep(stepOrder, Step1Id))
                .Add(AppendIntegerPipelineStep.CreateBuilderStep(stepOrder, Step2Id))
                .Add(AppendIntegerPipelineStep.CreateBuilderStep(stepOrder, Step3Id))
                .Build();

            Assert.NotNull(handler);

            Assert.Empty(stepOrder);

            handler.Invoke(new Exception(), new ExceptionPipelineExceptionContext(DateTime.UtcNow));

            Assert.Equal(new int[] { Step1Id, Step2Id, Step3Id }, stepOrder);
        }

        [Fact]
        public void ExceptionPipelineBuilder_StepShortCircuit()
        {
            const int Step1Id = 5;
            const int Step2Id = 8;
            const int Step3Id = 2;

            List<int> stepOrder = new List<int>();

            ExceptionPipelineDelegate handler = new ExceptionPipelineBuilder()
                .Add(AppendIntegerPipelineStep.CreateBuilderStep(stepOrder, Step1Id))
                .Add(AppendIntegerPipelineStep.CreateBuilderStep(stepOrder, Step2Id, shortCircuit: true))
                .Add(AppendIntegerPipelineStep.CreateBuilderStep(stepOrder, Step3Id))
                .Build();

            Assert.NotNull(handler);

            Assert.Empty(stepOrder);

            handler.Invoke(new Exception(), new ExceptionPipelineExceptionContext(DateTime.UtcNow));

            Assert.Equal(new int[] { Step1Id, Step2Id }, stepOrder);
        }
    }
}
