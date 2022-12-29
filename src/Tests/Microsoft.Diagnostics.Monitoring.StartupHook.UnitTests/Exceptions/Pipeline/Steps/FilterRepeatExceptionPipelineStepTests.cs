// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline.Steps
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMoniker.Current)]
    public sealed class FilterRepeatExceptionPipelineStepTests
    {
        private readonly List<Exception> _reportedExceptions = new();
        private readonly ExceptionPipelineDelegate _finalHandler;

        public FilterRepeatExceptionPipelineStepTests()
        {
            _finalHandler = _reportedExceptions.Add;
        }

        [Fact]
        public void FilterRepeatExceptionPipelineStep_AllowFirstOccurrences()
        {
            FilterRepeatExceptionPipelineStep action = new(_finalHandler);

            Exception firstException = new();
            action.Invoke(firstException);

            Exception secondException = new();
            action.Invoke(secondException);

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
            action.Invoke(firstException);
            action.Invoke(firstException);
            action.Invoke(firstException);

            Exception secondException = new();
            action.Invoke(secondException);
            action.Invoke(secondException);

            IEnumerable<Exception> allExceptions = new List<Exception>()
            {
                firstException,
                secondException
            };

            Assert.Equal(allExceptions, _reportedExceptions);
        }
    }
}
