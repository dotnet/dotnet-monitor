﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes;
using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.Pipeline;
using Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher.Models;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.UnitTests.ParameterCapturing.Pipeline
{
    internal sealed class TestFunctionProbesManager : IFunctionProbesManager
    {
        private readonly Action<IList<MethodInfo>> _onStart;
        private readonly Action _onStop;

        public TestFunctionProbesManager(Action<IList<MethodInfo>> onStart = null, Action onStop = null)
        {
            _onStart = onStart;
            _onStop = onStop;
        }

        public void StartCapturing(IList<MethodInfo> methods)
        {
            _onStart?.Invoke(methods);
        }

        public void StopCapturing()
        {
            _onStop?.Invoke();
        }

        public void Dispose()
        {
        }
    }

    internal sealed class TestParameterCapturingCallbacks : IParameterCapturingPipelineCallbacks
    {
        private readonly Action<StartCapturingParametersPayload, IList<MethodInfo>> _onCapturingStart;
        private readonly Action<Guid> _onCapturingStop;
        private readonly Action<Guid, ParameterCapturingEvents.CapturingFailedReason, string> _onCapturingFailed;

        public TestParameterCapturingCallbacks(
            Action<StartCapturingParametersPayload, IList<MethodInfo>> onCapturingStart = null,
            Action<Guid> onCapturingStop = null,
            Action<Guid, ParameterCapturingEvents.CapturingFailedReason, string> onCapturingFailed = null)
        {
            _onCapturingStart = onCapturingStart;
            _onCapturingStop = onCapturingStop;
            _onCapturingFailed = onCapturingFailed;
        }

        public void CapturingStart(StartCapturingParametersPayload request, IList<MethodInfo> methods)
        {
            _onCapturingStart?.Invoke(request, methods);
        }

        public void CapturingStop(Guid RequestId)
        {
            _onCapturingStop?.Invoke(RequestId);
        }

        public void FailedToCapture(Guid RequestId, ParameterCapturingEvents.CapturingFailedReason Reason, string Details)
        {
            _onCapturingFailed?.Invoke(RequestId, Reason, Details);
        }
    }

    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class ParameterCapturingPipelineTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public ParameterCapturingPipelineTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public void RequestStop_InvalidRequestId_Throws()
        {
            // Arrange
            ParameterCapturingPipeline pipeline = new(new TestFunctionProbesManager(), new TestParameterCapturingCallbacks());

            // Act & Assert
            Assert.Throws<ArgumentException>(() => pipeline.RequestStop(Guid.NewGuid()));
        }

        [Fact]
        public async Task Request_DoesInstallAndNotify()
        {
            // Arrange
            using CancellationTokenSource cts = new();
            cts.CancelAfter(CommonTestTimeouts.GeneralTimeout);

            TaskCompletionSource probeManagerStartSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<Guid> onStartCallbackSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            using IDisposable registration = cts.Token.Register(() =>
            {
                _ = probeManagerStartSource.TrySetCanceled(cts.Token);
                _ = onStartCallbackSource.TrySetCanceled(cts.Token);
            });

            TestFunctionProbesManager probeManager = new(
                onStart: (_) =>
                {
                    probeManagerStartSource.TrySetResult();
                });

            TestParameterCapturingCallbacks callbacks = new(
                onCapturingStart: (payload, _) =>
                {
                    onStartCallbackSource.TrySetResult(payload.RequestId);
                });

            ParameterCapturingPipeline pipeline = new(probeManager, callbacks);
            StartCapturingParametersPayload payload = CreateStartCapturingPayload(Timeout.InfiniteTimeSpan);

            Task pipelineTask = pipeline.RunAsync(cts.Token);

            // Act
            pipeline.SubmitRequest(payload);

            // Assert
            await probeManagerStartSource.Task;
            Guid startedRequest = await onStartCallbackSource.Task;
            Assert.Equal(payload.RequestId, startedRequest);
        }

        [Fact]
        public async Task UnresolvableMethod_DoesNotify()
        {
            // Arrange
            using CancellationTokenSource cts = new();
            cts.CancelAfter(CommonTestTimeouts.GeneralTimeout);

            TaskCompletionSource<(Guid, ParameterCapturingEvents.CapturingFailedReason, string)> onFailedCallbackSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
            using IDisposable registration = cts.Token.Register(() =>
            {
                _ = onFailedCallbackSource.TrySetCanceled(cts.Token);
            });

            TestFunctionProbesManager probeManager = new();

            TestParameterCapturingCallbacks callbacks = new(
                onCapturingFailed: (id, reason, details) =>
                {
                    onFailedCallbackSource.TrySetResult((id, reason, details));
                });

            ParameterCapturingPipeline pipeline = new(probeManager, callbacks);
            StartCapturingParametersPayload payload = new()
            {
                RequestId = Guid.NewGuid(),
                Duration = Timeout.InfiniteTimeSpan,
                Methods = new[]
                {
                    new MethodDescription()
                    {
                        AssemblyName = Guid.NewGuid().ToString("D"),
                        TypeName = Guid.NewGuid().ToString("D"),
                        MethodName = Guid.NewGuid().ToString("D")
                    }
                }
            };

            Task pipelineTask = pipeline.RunAsync(cts.Token);

            // Act
            pipeline.SubmitRequest(payload);

            // Assert
            (Guid requestId, ParameterCapturingEvents.CapturingFailedReason reason, string details) = await onFailedCallbackSource.Task;
            Assert.Equal(payload.RequestId, requestId);
            Assert.Equal(ParameterCapturingEvents.CapturingFailedReason.UnresolvedMethods, reason);
        }


        [Fact]
        public async Task RequestStop_DoesStopCapturingAndNotify()
        {
            // Arrange
            using CancellationTokenSource cts = new();
            cts.CancelAfter(CommonTestTimeouts.GeneralTimeout);

            TaskCompletionSource probeManagerStopSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<Guid> onStopCallbackSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            using IDisposable registration = cts.Token.Register(() =>
            {
                _ = probeManagerStopSource.TrySetCanceled(cts.Token);
                _ = onStopCallbackSource.TrySetCanceled(cts.Token);
            });

            TestFunctionProbesManager probeManager = new(
                onStop: () =>
                {
                    probeManagerStopSource.TrySetResult();
                });

            TestParameterCapturingCallbacks callbacks = new(
                onCapturingStop: (requestId) =>
                {
                    onStopCallbackSource.TrySetResult(requestId);
                });

            ParameterCapturingPipeline pipeline = new(probeManager, callbacks);
            StartCapturingParametersPayload payload = CreateStartCapturingPayload(Timeout.InfiniteTimeSpan);

            Task pipelineTask = pipeline.RunAsync(cts.Token);
            pipeline.SubmitRequest(payload);

            // Act
            pipeline.RequestStop(payload.RequestId);

            // Assert
            await probeManagerStopSource.Task;
            Guid stoppedRequest = await onStopCallbackSource.Task;
            Assert.Equal(payload.RequestId, stoppedRequest);
        }

        [Fact]
        public async Task Request_StopsAfterDuration()
        {
            // Arrange
            using CancellationTokenSource cts = new();
            cts.CancelAfter(CommonTestTimeouts.GeneralTimeout);

            TaskCompletionSource<Guid> onStopCallbackSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            using IDisposable registration = cts.Token.Register(() =>
            {
                _ = onStopCallbackSource.TrySetCanceled(cts.Token);
            });

            TestFunctionProbesManager probeManager = new();

            TestParameterCapturingCallbacks callbacks = new(
                onCapturingStop: (requestId) =>
                {
                    onStopCallbackSource.TrySetResult(requestId);
                });

            ParameterCapturingPipeline pipeline = new(probeManager, callbacks);
            StartCapturingParametersPayload payload = CreateStartCapturingPayload(TimeSpan.FromSeconds(1));

            Task pipelineTask = pipeline.RunAsync(cts.Token);

            // Act
            pipeline.SubmitRequest(payload);

            // Assert
            Guid stoppedRequest = await onStopCallbackSource.Task;
            Assert.Equal(payload.RequestId, stoppedRequest);
        }

        [Fact]
        public async Task RunAsync_ThrowsOnCapturingStopFailure()
        {
            // Arrange
            using CancellationTokenSource cts = new();
            cts.CancelAfter(CommonTestTimeouts.GeneralTimeout);

            Exception thrownException = new Exception("test");
            TestFunctionProbesManager probeManager = new(
                onStop: () =>
                {
                    throw thrownException;
                });

            TestParameterCapturingCallbacks callbacks = new();

            ParameterCapturingPipeline pipeline = new(probeManager, callbacks);
            StartCapturingParametersPayload payload = CreateStartCapturingPayload(TimeSpan.FromSeconds(1));

            // Act
            Task pipelineTask = pipeline.RunAsync(cts.Token);
            pipeline.SubmitRequest(payload);

            // Assert
            Exception ex = await Assert.ThrowsAsync<Exception>(() => pipelineTask).WaitAsync(cts.Token);
            Assert.Equal(ex, thrownException);
        }

        private StartCapturingParametersPayload CreateStartCapturingPayload(TimeSpan duration)
        {
            string assemblyName = typeof(ParameterCapturingPipelineTests).Assembly.GetName()?.Name;
            Assert.NotNull(assemblyName);

            string typeName = typeof(ParameterCapturingPipelineTests).FullName;
            Assert.NotNull(typeName);

            return new StartCapturingParametersPayload()
            {
                RequestId = Guid.NewGuid(),
                Duration = duration,
                Methods = new[]
                {
                    new MethodDescription()
                    {
                        AssemblyName = assemblyName,
                        TypeName = typeName,
                        MethodName = nameof(CreateStartCapturingPayload)
                    }
                }
            };
        }
    }
}
