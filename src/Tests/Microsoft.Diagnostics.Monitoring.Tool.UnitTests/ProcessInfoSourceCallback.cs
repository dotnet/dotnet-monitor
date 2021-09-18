// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    internal sealed class ProcessInfoSourceCallback : IProcessInfoSourceCallbacks
    {
        private readonly ITestOutputHelper _outputHelper;
        /// <summary>
        /// Used to protect the completion list from mutation while processing
        /// callbacks from it. The processing is done in an async method with async
        /// calls, which are not allowed in a lock, thus use SemaphoreSlim.
        /// </summary>
        private readonly SemaphoreSlim _completionEntriesSemaphore = new(1);
        private readonly List<CompletionEntry> _completionEntries = new();

        public ProcessInfoSourceCallback(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        public async Task<IProcessInfo> WaitForNewProcessInfoAsync(AppRunner runner, TimeSpan timeout)
        {
            CompletionEntry entry = new(runner);
            using CancellationTokenSource timeoutCancellation = new(timeout);

            await _completionEntriesSemaphore.WaitAsync(timeoutCancellation.Token);
            try
            {
                _completionEntries.Add(entry);
                _outputHelper.WriteLine($"[Wait] Register App{runner.AppId}");
            }
            finally
            {
                _completionEntriesSemaphore.Release();
            }

            _outputHelper.WriteLine($"[Wait] Wait for App{runner.AppId} notification");
            IProcessInfo processInfo = await entry.WithCancellation(timeoutCancellation.Token);
            _outputHelper.WriteLine($"[Wait] Received App{runner.AppId} notification");

            return processInfo;
        }

        public Task OnBeforeResumeAsync(IProcessInfo processInfo, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        public async Task OnAddedProcessInfoAsync(IProcessInfo processInfo, CancellationToken token)
        {
            _outputHelper.WriteLine($"[Source] Added: {ToOutputString(processInfo)}");

            await _completionEntriesSemaphore.WaitAsync(token);
            try
            {
                _outputHelper.WriteLine($"[Source] Start notifications for process {processInfo.ProcessId}");

                // Create a mapping of the process ID tasks to the completion entries
                IDictionary<Task<int>, CompletionEntry> map = new Dictionary<Task<int>, CompletionEntry>(_completionEntries.Count);
                foreach (CompletionEntry entry in _completionEntries)
                {
                    map.Add(entry.Runner.ProcessIdTask.WithCancellation(token), entry);
                }

                while (map.Count > 0)
                {
                    // Wait for any of the process ID tasks to complete.
                    Task<int> completedTask = await Task.WhenAny(map.Keys);

                    map.Remove(completedTask, out CompletionEntry entry);

                    _outputHelper.WriteLine($"[Source] Checking App{entry.Runner.AppId}");

                    if (completedTask.IsCompletedSuccessfully)
                    {
                        // If the process ID matches the one that was reported via the callback,
                        // then signal its completion source.
                        if (processInfo.ProcessId == completedTask.Result)
                        {
                            _outputHelper.WriteLine($"[Source] Notifying App{entry.Runner.AppId}");
                            entry.Complete(processInfo);

                            _completionEntries.Remove(entry);

                            break;
                        }
                    }
                }

                _outputHelper.WriteLine($"[Source] Finished notifications for process {processInfo.ProcessId}");
            }
            finally
            {
                _completionEntriesSemaphore.Release();
            }
        }

        public void OnRemovedProcessInfo(IProcessInfo processInfo)
        {
            _outputHelper.WriteLine($"[Source] Removed: {ToOutputString(processInfo)}");
        }

        private static string ToOutputString(IProcessInfo processInfo)
        {
            return FormattableString.Invariant($"PID={processInfo.ProcessId}, Cookie={processInfo.RuntimeInstanceCookie}");
        }

        private sealed class CompletionEntry
        {
            private readonly TaskCompletionSource<IProcessInfo> _completionSource =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            public CompletionEntry(AppRunner runner)
            {
                Runner = runner;
            }

            public AppRunner Runner { get; }

            public void Complete(IProcessInfo processInfo)
            {
                _completionSource.TrySetResult(processInfo);
            }

            public Task<IProcessInfo> WithCancellation(CancellationToken token)
            {
                return _completionSource.WithCancellation(token);
            }
        }
    }
}
