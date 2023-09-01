// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline.Steps
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class ExceptionDemultiplexerPipelineStepTests
    {
        private readonly Dictionary<Exception, int> _reportedExceptionCounts = new();
        private readonly ExceptionPipelineDelegate _finalHandler;

        public ExceptionDemultiplexerPipelineStepTests()
        {
            _finalHandler = (ex, context) =>
            {
                _reportedExceptionCounts[ex] = _reportedExceptionCounts.GetValueOrDefault(ex) + 1;
            };
        }

        [Fact]
        public void ExceptionDemultiplexerPipelineStep_SimpleException()
        {
            ExceptionDemultiplexerPipelineStep action = new(_finalHandler);

            Exception ex = new();
            ExceptionPipelineExceptionContext context = new(DateTime.UtcNow);

            action.Invoke(ex, context);

            (Exception reportedException, int exceptionCount) = Assert.Single(_reportedExceptionCounts);
            Assert.Equal(ex, reportedException);
            Assert.Equal(1, exceptionCount);
        }

        [Fact]
        public void ExceptionDemultiplexerPipelineStep_InnerException()
        {
            ExceptionDemultiplexerPipelineStep action = new(_finalHandler);

            Exception innerException = new();
            Exception outerException = new(null, innerException);
            ExceptionPipelineExceptionContext context = new(DateTime.UtcNow);

            action.Invoke(outerException, context);

            Assert.Equal(2, _reportedExceptionCounts.Count);

            Assert.True(_reportedExceptionCounts.TryGetValue(innerException, out int innerCount));
            Assert.Equal(1, innerCount);

            Assert.True(_reportedExceptionCounts.TryGetValue(outerException, out int outerCount));
            Assert.Equal(1, outerCount);
        }

        [Fact]
        public void ExceptionDemultiplexerPipelineStep_DeepInnerException()
        {
            ExceptionDemultiplexerPipelineStep action = new(_finalHandler);

            Exception innerInnerException = new();
            Exception innerException = new(null, innerInnerException);
            Exception outerException = new(null, innerException);
            ExceptionPipelineExceptionContext context = new(DateTime.UtcNow);

            action.Invoke(outerException, context);

            Assert.Equal(3, _reportedExceptionCounts.Count);

            Assert.True(_reportedExceptionCounts.TryGetValue(innerInnerException, out int innerInnerCount));
            Assert.Equal(1, innerInnerCount);

            Assert.True(_reportedExceptionCounts.TryGetValue(innerException, out int innerCount));
            Assert.Equal(1, innerCount);

            Assert.True(_reportedExceptionCounts.TryGetValue(outerException, out int outerCount));
            Assert.Equal(1, outerCount);
        }

        [Fact]
        public void ExceptionDemultiplexerPipelineStep_AggregateException()
        {
            ExceptionDemultiplexerPipelineStep action = new(_finalHandler);

            Exception innerException1 = new();
            Exception innerException2 = new();
            Exception innerException3 = new();
            Exception outerException = new AggregateException(innerException1, innerException2, innerException3);
            ExceptionPipelineExceptionContext context = new(DateTime.UtcNow);

            action.Invoke(outerException, context);

            Assert.Equal(4, _reportedExceptionCounts.Count);

            // While the InnerException is the same as the first exception in InnerExceptions, the
            // pipeline step is expected to only report it once.
            Assert.True(_reportedExceptionCounts.TryGetValue(innerException1, out int inner1Count));
            Assert.Equal(1, inner1Count);

            Assert.True(_reportedExceptionCounts.TryGetValue(innerException2, out int inner2Count));
            Assert.Equal(1, inner2Count);

            Assert.True(_reportedExceptionCounts.TryGetValue(innerException3, out int inner3Count));
            Assert.Equal(1, inner3Count);

            Assert.True(_reportedExceptionCounts.TryGetValue(outerException, out int outerCount));
            Assert.Equal(1, outerCount);
        }

        [Fact]
        public void ExceptionDemultiplexerPipelineStep_ReflectionTypeLoadException()
        {
            ExceptionDemultiplexerPipelineStep action = new(_finalHandler);

            Exception innerException1 = new();
            Exception innerException2 = new();
            Exception outerException = new ReflectionTypeLoadException(
                classes: null,
                exceptions: new Exception?[]
                {
                    innerException1,
                    innerException2,
                    null
                });
            ExceptionPipelineExceptionContext context = new(DateTime.UtcNow);

            action.Invoke(outerException, context);

            Assert.Equal(3, _reportedExceptionCounts.Count);

            Assert.True(_reportedExceptionCounts.TryGetValue(innerException1, out int inner1Count));
            Assert.Equal(1, inner1Count);

            Assert.True(_reportedExceptionCounts.TryGetValue(innerException2, out int inner2Count));
            Assert.Equal(1, inner2Count);

            Assert.True(_reportedExceptionCounts.TryGetValue(outerException, out int outerCount));
            Assert.Equal(1, outerCount);
        }
    }
}
