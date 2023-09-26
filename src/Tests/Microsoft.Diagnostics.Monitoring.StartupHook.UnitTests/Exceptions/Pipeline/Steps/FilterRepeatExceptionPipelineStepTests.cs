// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline.Steps
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class FilterRepeatExceptionPipelineStepTests
    {
        private readonly List<Exception> _reportedExceptions = new();
        private readonly ExceptionPipelineDelegate _finalHandler;

        public FilterRepeatExceptionPipelineStepTests()
        {
            _finalHandler = (ex, context) => _reportedExceptions.Add(ex);
        }

        [Fact]
        public void FilterRepeatExceptionPipelineStep_AllowFirstOccurrences()
        {
            FilterRepeatExceptionPipelineStep action = new(_finalHandler);

            Exception firstException = new();
            ExceptionPipelineExceptionContext firstContext = new(DateTime.UtcNow);

            action.Invoke(firstException, firstContext);

            Exception secondException = new();
            ExceptionPipelineExceptionContext secondContext = new(DateTime.UtcNow);

            action.Invoke(secondException, secondContext);

            IEnumerable<Exception> allExceptions = new List<Exception>()
            {
                firstException,
                secondException
            };

            Assert.Equal(allExceptions, _reportedExceptions);
        }

        [Fact]
        public void FilterRepeatExceptionPipelineStep_FilterSubsequentOccurrences()
        {
            FilterRepeatExceptionPipelineStep action = new(_finalHandler);

            Exception firstException = new();

            ExceptionPipelineExceptionContext firstContext = new(DateTime.UtcNow);
            action.Invoke(firstException, firstContext);
            firstContext = new ExceptionPipelineExceptionContext(DateTime.UtcNow);
            action.Invoke(firstException, firstContext);
            firstContext = new ExceptionPipelineExceptionContext(DateTime.UtcNow);
            action.Invoke(firstException, firstContext);

            Exception secondException = new();

            ExceptionPipelineExceptionContext secondContext = new(DateTime.UtcNow);
            action.Invoke(secondException, secondContext);
            secondContext = new ExceptionPipelineExceptionContext(DateTime.UtcNow);
            action.Invoke(secondException, secondContext);

            IEnumerable<Exception> allExceptions = new List<Exception>()
            {
                firstException,
                secondException
            };

            Assert.Equal(allExceptions, _reportedExceptions);
        }
    }
}
