// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline.Steps
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class FilterMonitorExceptionPipelineStepTests
    {
        private readonly List<Exception> _reportedExceptions = new();
        private readonly ExceptionPipelineDelegate _finalHandler;

        public FilterMonitorExceptionPipelineStepTests()
        {
            _finalHandler = (ex, context) => _reportedExceptions.Add(ex);
        }

        [Fact]
        public void PassesThroughOtherExceptions()
        {
            FilterMonitorExceptionPipelineStep action = new(_finalHandler);

            Exception firstException = new();
            ExceptionPipelineExceptionContext firstContext = new(DateTime.UtcNow);

            action.Invoke(firstException, firstContext);

            IEnumerable<Exception> allExceptions = new List<Exception>()
            {
                firstException
            };

            Assert.Equal(allExceptions, _reportedExceptions);
        }

        [Fact]
        public void FiltersMonitorExceptions()
        {
            FilterMonitorExceptionPipelineStep action = new(_finalHandler);

            Exception firstException = new();

            ExceptionPipelineExceptionContext firstContext = new(DateTime.UtcNow);
            using (IDisposable _ = MonitorExecutionContextTracker.MonitorScope())
            {
                action.Invoke(firstException, firstContext);
            }

            IEnumerable<Exception> allExceptions = new List<Exception>()
            {
            };

            Assert.Equal(allExceptions, _reportedExceptions);
        }
    }
}
