// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.Eventing
{
    internal sealed class AsyncParameterCapturingEventSource : IDisposable
    {
        private readonly Thread _thread;
        private readonly CancellationTokenSource _cts = new();
        private readonly BlockingCollection<Action<CancellationToken>> _pendingEvents = new(MaxPendingEventsCollectionCapacity);
        private readonly ParameterCapturingEventSource _eventSource;

        private const int MaxPendingEventsCollectionCapacity = 1024;
        private const string BackgroundLoggingThreadName = "[dotnet-monitor] Parameter Capturing EventSource";
        private long _disposedState;

        public AsyncParameterCapturingEventSource(ParameterCapturingEventSource eventSource)
        {
            _eventSource = eventSource;

            _thread = new(ThreadLoop)
            {
                Priority = ThreadPriority.BelowNormal,
                IsBackground = true,
                Name = BackgroundLoggingThreadName
            };
            _thread.Start();
        }

        public void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
            {
                return;
            }

            _pendingEvents.CompleteAdding();
            try
            {
                _cts.Cancel();
            }
            catch
            {
            }

            _thread.Join();

            _cts.Dispose();
            _pendingEvents.Dispose();
        }

        public bool IsEnabled => _eventSource.IsEnabled();

        public void OnCapturedParameters(
            Guid requestId,
            string methodName,
            string methodModuleName,
            string? methodDeclaringTypeName,
            ResolvedParameterInfo[] parameters
            )
        {
            Activity? currentActivity = Activity.Current;
            int currentThreadId = Environment.CurrentManagedThreadId;

            ScheduleAction((cancellationToken) =>
            {
                Guid captureId = Guid.NewGuid();

                cancellationToken.ThrowIfCancellationRequested();

                _eventSource.CapturedParameterStart(
                    requestId,
                    captureId,
                    currentActivity?.Id ?? string.Empty,
                    currentActivity?.IdFormat ?? ActivityIdFormat.Unknown,
                    currentThreadId,
                    methodName,
                    methodModuleName,
                    methodDeclaringTypeName ?? string.Empty);

                foreach (ResolvedParameterInfo param in parameters)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _eventSource.CapturedParameter(
                        requestId,
                        captureId,
                        param.Name ?? string.Empty,
                        param.Type ?? string.Empty,
                        param.TypeModuleName ?? string.Empty,
                        param.Value.FormattedValue,
                        param.Value.EvalResult,
                        param.Attributes,
                        param.IsByRef);
                }

                cancellationToken.ThrowIfCancellationRequested();
                _eventSource.CapturedParameterStop(requestId, captureId);
            });
        }

        private void ScheduleAction(Action<CancellationToken> action)
        {
            _ = _pendingEvents.TryAdd(action);
        }

        private void ThreadLoop()
        {
            using IDisposable _ = MonitorExecutionContextTracker.MonitorScope();
            try
            {
                while (_pendingEvents.TryTake(out Action<CancellationToken>? eventAction, Timeout.Infinite, _cts.Token))
                {
                    eventAction(_cts.Token);
                }
            }
            catch
            {
            }
        }
    }
}
