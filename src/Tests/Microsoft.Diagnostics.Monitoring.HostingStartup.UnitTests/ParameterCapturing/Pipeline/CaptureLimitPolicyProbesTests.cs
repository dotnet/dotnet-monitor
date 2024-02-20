// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.Pipeline;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.UnitTests.ParameterCapturing.Pipeline
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class CaptureLimitPolicyProbesTests
    {
        [Fact]
        public void EnterProbe_RequestStop_OnLimitReached()
        {
            // Arrange
            TaskCompletionSource requestStop = new();
            CaptureLimitPolicyProbes probes = new(new TestFunctionProbes(), captureLimit: 1, requestStop);

            // Act
            probes.EnterProbe(1, []);

            // Assert
            Assert.True(requestStop.Task.IsCompleted);
        }

        [Fact]
        public void EnterProbe_DoesNotRequestStop_WhenLimitNotReached()
        {
            // Arrange
            TaskCompletionSource requestStop = new();
            CaptureLimitPolicyProbes probes = new(new TestFunctionProbes(), captureLimit: 2, requestStop);

            // Act
            probes.EnterProbe(1, []);

            // Assert
            Assert.False(requestStop.Task.IsCompleted);
        }

        [Fact]
        public void EnterProbe_DoesNotRequestStop_WhenProbeDoesNotCapture()
        {
            // Arrange
            TestFunctionProbes testProbes = new(onEnterProbe: (_, _) =>
            {
                return false;
            });

            TaskCompletionSource requestStop = new();
            CaptureLimitPolicyProbes probes = new(testProbes, captureLimit: 1, requestStop);

            // Act
            probes.EnterProbe(1, []);

            // Assert
            Assert.False(requestStop.Task.IsCompleted);
        }

        [Fact]
        public void EnterProbe_ShortCircuits_WhenLimitReached()
        {
            // Arrange
            int invokeCount = 0;
            TestFunctionProbes testProbes = new(onEnterProbe: (_, _) =>
            {
                Interlocked.Increment(ref invokeCount);
                return true;
            });

            TaskCompletionSource requestStop = new();
            CaptureLimitPolicyProbes probes = new(testProbes, captureLimit: 1, requestStop);

            // Act
            probes.EnterProbe(1, []);
            probes.EnterProbe(2, []);

            // Assert
            Assert.Equal(1, invokeCount);
        }
    }
}
