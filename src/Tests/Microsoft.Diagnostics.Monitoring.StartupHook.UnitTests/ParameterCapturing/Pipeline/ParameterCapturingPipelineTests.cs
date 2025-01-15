// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing;
using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.Boxing;
using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.FunctionProbes;
using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.Pipeline;
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

namespace Microsoft.Diagnostics.Monitoring.StartupHook.UnitTests.ParameterCapturing.Pipeline
{
    internal sealed class TestFunctionProbesManager : IFunctionProbesManager
    {
        private readonly Action<IList<MethodInfo>, IFunctionProbes>? _onStart;
        private readonly Action? _onStop;

        public event EventHandler<InstrumentedMethod>? OnProbeFault;

        public TestFunctionProbesManager(Action<IList<MethodInfo>, IFunctionProbes>? onStart = null, Action? onStop = null)
        {
            _onStart = onStart;
            _onStop = onStop;
        }

        public void TriggerFault(MethodInfo method)
        {
            InstrumentedMethod faultingMethod = new(method, BoxingInstructions.GetBoxingInstructions(method));
            OnProbeFault?.Invoke(this, faultingMethod);
        }

        public Task StartCapturingAsync(IList<MethodInfo> methods, IFunctionProbes probes, CancellationToken token)
        {
            _onStart?.Invoke(methods, probes);
            return Task.CompletedTask;
        }

        public Task StopCapturingAsync(CancellationToken token)
        {
            _onStop?.Invoke();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }

    internal sealed class TestParameterCapturingCallbacks : IParameterCapturingPipelineCallbacks
    {
        private readonly Action<StartCapturingParametersPayload, IList<MethodInfo>>? _onCapturingStart;
        private readonly Action<Guid>? _onCapturingStop;
        private readonly Action<Guid, ParameterCapturingEvents.CapturingFailedReason, string>? _onCapturingFailed;
        private readonly Action<Guid, InstrumentedMethod>? _onProbeFault;

        public TestParameterCapturingCallbacks(
            Action<StartCapturingParametersPayload, IList<MethodInfo>>? onCapturingStart = null,
            Action<Guid>? onCapturingStop = null,
            Action<Guid, ParameterCapturingEvents.CapturingFailedReason, string>? onCapturingFailed = null,
            Action<Guid, InstrumentedMethod>? onProbeFault = null)
        {
            _onCapturingStart = onCapturingStart;
            _onCapturingStop = onCapturingStop;
            _onCapturingFailed = onCapturingFailed;
            _onProbeFault = onProbeFault;
        }

        public void CapturingStart(StartCapturingParametersPayload request, IList<MethodInfo> methods)
        {
            _onCapturingStart?.Invoke(request, methods);
        }

        public void CapturingStop(Guid requestId)
        {
            _onCapturingStop?.Invoke(requestId);
        }

        public void FailedToCapture(Guid requestId, ParameterCapturingEvents.CapturingFailedReason reason, string details)
        {
            _onCapturingFailed?.Invoke(requestId, reason, details);
        }

        public void ProbeFault(Guid requestId, InstrumentedMethod faultingMethod)
        {
            _onProbeFault?.Invoke(requestId, faultingMethod);
        }
    }

    internal sealed class TestMethodDescriptionValidator : IMethodDescriptionValidator
    {
        private readonly Func<MethodDescription, bool>? _onValidateMethods;
        public TestMethodDescriptionValidator(Func<MethodDescription, bool>? onValidateMethods = null)
        {
            _onValidateMethods = onValidateMethods;
        }
        public bool IsMethodDescriptionAllowed(MethodDescription methodDescription)
        {
            if (_onValidateMethods == null)
            {
                return true;
            }

            return _onValidateMethods(methodDescription);
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
            ParameterCapturingPipeline pipeline = new(new TestFunctionProbesManager(), new TestParameterCapturingCallbacks(), new TestMethodDescriptionValidator());

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

            TestFunctionProbes probes = new();

            TestFunctionProbesManager probeManager = new(
                onStart: (_, _) =>
                {
                    probeManagerStartSource.TrySetResult();
                });

            TestParameterCapturingCallbacks callbacks = new(
                onCapturingStart: (payload, _) =>
                {
                    onStartCallbackSource.TrySetResult(payload.RequestId);
                });

            ParameterCapturingPipeline pipeline = new(probeManager, callbacks, new TestMethodDescriptionValidator());
            StartCapturingParametersPayload payload = CreateStartCapturingPayload(Timeout.InfiniteTimeSpan);

            Task pipelineTask = pipeline.RunAsync(cts.Token);

            // Act
            pipeline.SubmitRequest(payload, probes);

            // Assert
            await probeManagerStartSource.Task;
            Guid startedRequest = await onStartCallbackSource.Task;
            Assert.Equal(payload.RequestId, startedRequest);
        }

        [Fact]
        public void Request_DoesRejectDenyListMatch()
        {
            // Arrange
            TestMethodDescriptionValidator methodDescriptionValidator = new(
                onValidateMethods: (_) =>
                {
                    return false;
                });

            ParameterCapturingPipeline pipeline = new(new TestFunctionProbesManager(), new TestParameterCapturingCallbacks(), methodDescriptionValidator);
            StartCapturingParametersPayload payload = CreateStartCapturingPayload(Timeout.InfiniteTimeSpan);
            TestFunctionProbes probes = new();

            // Act & Assert
            Assert.Throws<DeniedMethodsException>(() => pipeline.SubmitRequest(payload, probes));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Request_InvalidCaptureLimit_Throws(int captureLimit)
        {
            // Arrange
            ParameterCapturingPipeline pipeline = new(new TestFunctionProbesManager(), new TestParameterCapturingCallbacks(), new TestMethodDescriptionValidator());
            TestFunctionProbes probes = new();
            StartCapturingParametersPayload payload = CreateStartCapturingPayload(Timeout.InfiniteTimeSpan, captureLimit: captureLimit);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => pipeline.SubmitRequest(payload, probes));
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

            TestFunctionProbes probes = new();
            TestFunctionProbesManager probeManager = new();

            TestParameterCapturingCallbacks callbacks = new(
                onCapturingFailed: (id, reason, details) =>
                {
                    onFailedCallbackSource.TrySetResult((id, reason, details));
                });

            ParameterCapturingPipeline pipeline = new(probeManager, callbacks, new TestMethodDescriptionValidator());
            StartCapturingParametersPayload payload = new()
            {
                RequestId = Guid.NewGuid(),
                Duration = Timeout.InfiniteTimeSpan,
                Configuration =
                {
                    Methods = new[]
                    {
                        new MethodDescription()
                        {
                            ModuleName = Guid.NewGuid().ToString("D"),
                            TypeName = Guid.NewGuid().ToString("D"),
                            MethodName = Guid.NewGuid().ToString("D")
                        }
                    }
                }
            };

            Task pipelineTask = pipeline.RunAsync(cts.Token);

            // Act
            pipeline.SubmitRequest(payload, probes);

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

            TestFunctionProbes probes = new();

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

            ParameterCapturingPipeline pipeline = new(probeManager, callbacks, new TestMethodDescriptionValidator());
            StartCapturingParametersPayload payload = CreateStartCapturingPayload(Timeout.InfiniteTimeSpan);

            Task pipelineTask = pipeline.RunAsync(cts.Token);
            pipeline.SubmitRequest(payload, probes);

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

            TestFunctionProbes probes = new();
            TestFunctionProbesManager probeManager = new();

            TestParameterCapturingCallbacks callbacks = new(
                onCapturingStop: (requestId) =>
                {
                    onStopCallbackSource.TrySetResult(requestId);
                });

            ParameterCapturingPipeline pipeline = new(probeManager, callbacks, new TestMethodDescriptionValidator());
            StartCapturingParametersPayload payload = CreateStartCapturingPayload(TimeSpan.FromSeconds(1));

            Task pipelineTask = pipeline.RunAsync(cts.Token);

            // Act
            pipeline.SubmitRequest(payload, probes);

            // Assert
            Guid stoppedRequest = await onStopCallbackSource.Task;
            Assert.Equal(payload.RequestId, stoppedRequest);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(10)]
        public async Task Request_StopsAfterCaptureLimit(int captureLimit)
        {
            // Arrange
            using CancellationTokenSource cts = new();
            cts.CancelAfter(CommonTestTimeouts.GeneralTimeout);

            TaskCompletionSource<Guid> onStopCallbackSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<IFunctionProbes> onStartCallbackSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            using IDisposable registration = cts.Token.Register(() =>
            {
                _ = onStartCallbackSource.TrySetCanceled(cts.Token);
                _ = onStopCallbackSource.TrySetCanceled(cts.Token);
            });

            TestFunctionProbes probes = new();
            TestFunctionProbesManager probeManager = new(
                onStart: (_, probes) =>
                {
                    onStartCallbackSource.TrySetResult(probes);
                });

            TestParameterCapturingCallbacks callbacks = new(
                onCapturingStop: (requestId) =>
                {
                    onStopCallbackSource.TrySetResult(requestId);
                });

            ParameterCapturingPipeline pipeline = new(probeManager, callbacks, new TestMethodDescriptionValidator());
            StartCapturingParametersPayload payload = CreateStartCapturingPayload(Timeout.InfiniteTimeSpan, captureLimit: captureLimit);

            Task pipelineTask = pipeline.RunAsync(cts.Token);
            pipeline.SubmitRequest(payload, probes);
            IFunctionProbes pipelineProbes = await onStartCallbackSource.Task;

            // Act
            for (int i = 0; i < captureLimit; i++)
            {
                pipelineProbes.EnterProbe((ulong)i, []);
            }


            // Assert
            Guid stoppedRequest = await onStopCallbackSource.Task;
            Assert.Equal(payload.RequestId, stoppedRequest);
        }

        [Fact]
        public async Task ProbeFault_DoesNotify()
        {
            // Arrange
            using CancellationTokenSource cts = new();
            cts.CancelAfter(CommonTestTimeouts.GeneralTimeout);

            TaskCompletionSource<(Guid, IList<MethodInfo>)> onStartCallbackSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<(Guid, InstrumentedMethod)> onProbeFaultCallbackSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            using IDisposable registration = cts.Token.Register(() =>
            {
                _ = onStartCallbackSource.TrySetCanceled(cts.Token);
                _ = onProbeFaultCallbackSource.TrySetCanceled(cts.Token);
            });

            TestFunctionProbes probes = new();
            TestFunctionProbesManager probeManager = new();

            TestParameterCapturingCallbacks callbacks = new(
                onCapturingStart: (payload, methods) =>
                {
                    onStartCallbackSource.TrySetResult((payload.RequestId, methods));
                },
                onProbeFault: (requestId, faultingMethod) =>
                {
                    onProbeFaultCallbackSource.TrySetResult((requestId, faultingMethod));
                });

            ParameterCapturingPipeline pipeline = new(probeManager, callbacks, new TestMethodDescriptionValidator());
            StartCapturingParametersPayload payload = CreateStartCapturingPayload(Timeout.InfiniteTimeSpan);

            Task pipelineTask = pipeline.RunAsync(cts.Token);
            pipeline.SubmitRequest(payload, probes);
            (Guid startedRequest, IList<MethodInfo> methods) = await onStartCallbackSource.Task;
            Assert.Equal(payload.RequestId, startedRequest);
            MethodInfo instrumentedMethod = Assert.Single(methods);

            // Act
            probeManager.TriggerFault(instrumentedMethod);

            // Assert
            (Guid faultingRequest, InstrumentedMethod faultingMethod) = await onProbeFaultCallbackSource.Task;
            Assert.Equal(payload.RequestId, faultingRequest);
            Assert.Equal(instrumentedMethod.GetFunctionId(), faultingMethod.FunctionId);
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

            TestFunctionProbes probes = new();
            TestParameterCapturingCallbacks callbacks = new();

            ParameterCapturingPipeline pipeline = new(probeManager, callbacks, new TestMethodDescriptionValidator());
            StartCapturingParametersPayload payload = CreateStartCapturingPayload(TimeSpan.FromSeconds(1));

            // Act
            Task pipelineTask = pipeline.RunAsync(cts.Token);
            pipeline.SubmitRequest(payload, probes);

            // Assert
            Exception ex = await Assert.ThrowsAsync<Exception>(() => pipelineTask).WaitAsync(cts.Token);
            Assert.Equal(ex, thrownException);
        }

        private StartCapturingParametersPayload CreateStartCapturingPayload(TimeSpan duration, int? captureLimit = null)
        {
            string moduleName = typeof(ParameterCapturingPipelineTests).Module.Name;
            Assert.NotNull(moduleName);

            string? typeName = typeof(ParameterCapturingPipelineTests).FullName;
            Assert.NotNull(typeName);

            return new StartCapturingParametersPayload()
            {
                RequestId = Guid.NewGuid(),
                Duration = duration,
                Configuration =
                {
                    Methods = new[]
                    {
                        new MethodDescription()
                        {
                            ModuleName = moduleName,
                            TypeName = typeName,
                            MethodName = nameof(CreateStartCapturingPayload)
                        }
                    },
                    CaptureLimit = captureLimit
                }
            };
        }
    }
}
