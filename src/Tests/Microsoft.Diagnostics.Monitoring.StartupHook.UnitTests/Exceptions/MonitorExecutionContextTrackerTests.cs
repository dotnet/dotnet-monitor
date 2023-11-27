// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline.Steps
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class MonitorExecutionContextTrackerTests
    {
        [Fact]
        public void MonitorScope_IsActive()
        {
            // Arrange
            using (var scope = MonitorExecutionContextTracker.MonitorScope())
            {
                // Act
                bool result = MonitorExecutionContextTracker.IsInMonitorContext();

                // Assert
                Assert.True(result);
            }
        }

        [Fact]
        public void MonitorScope_IsDisposed()
        {
            // Arrange
            var scope = MonitorExecutionContextTracker.MonitorScope();
            scope.Dispose();

            // Act
            bool result = MonitorExecutionContextTracker.IsInMonitorContext();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void MonitorScope_Nested()
        {
            using (var scope1 = MonitorExecutionContextTracker.MonitorScope())
            {
                using (var scope2 = MonitorExecutionContextTracker.MonitorScope())
                {
                    Assert.True(MonitorExecutionContextTracker.IsInMonitorContext());
                }

                Assert.True(MonitorExecutionContextTracker.IsInMonitorContext());
            }

            Assert.False(MonitorExecutionContextTracker.IsInMonitorContext());
        }
    }
}
