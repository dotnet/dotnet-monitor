// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal class EndpointInfoSourceCallback : IEndpointInfoSourceCallbacks
    {
        private const string AddOperationName = "Add";
        private const string RemoveOperationName = "Remove";

        private readonly ITestOutputHelper _outputHelper;
        /// <summary>
        /// Used to protect the completion list from mutation while processing
        /// callbacks from it. The processing is done in an async method with async
        /// calls, which are not allowed in a lock, thus use SemaphoreSlim.
        /// </summary>
        private readonly SemaphoreSlim _addedEndpointEntriesSemaphore = new(1);
        private readonly List<CompletionEntry> _addedEndpointEntries = new();

        /// <summary>
        /// Used to protect the completion list from mutation while processing
        /// callbacks from it. The processing is done in an async method with async
        /// calls, which are not allowed in a lock, thus use SemaphoreSlim.
        /// </summary>
        private readonly SemaphoreSlim _removedEndpointEntriesSemaphore = new(1);
        private readonly List<CompletionEntry> _removedEndpointEntries = new();

        // Path to a startup hook assembly that will be applied to the target runtime
        // before it is resumed.
        private readonly string _startupHookPath;

        public EndpointInfoSourceCallback(ITestOutputHelper outputHelper, string startupHookPath = null)
        {
            _outputHelper = outputHelper;
            _startupHookPath = startupHookPath;
        }

        public Task<IEndpointInfo> WaitAddedEndpointInfoAsync(AppRunner runner, TimeSpan timeout)
        {
            return WaitForCompletionAsync(
                AddOperationName,
                _addedEndpointEntriesSemaphore,
                _addedEndpointEntries,
                _outputHelper,
                runner,
                timeout);
        }

        public async Task<IProcessInfo> WaitAddedProcessInfoAsync(AppRunner runner, TimeSpan timeout)
        {
            IEndpointInfo endpointInfo = await WaitAddedEndpointInfoAsync(runner, timeout);
            return await ProcessInfoImpl.FromEndpointInfoAsync(endpointInfo, timeout);
        }

        public Task<IEndpointInfo> WaitRemovedEndpointInfoAsync(AppRunner runner, TimeSpan timeout)
        {
            return WaitForCompletionAsync(
                RemoveOperationName,
                _removedEndpointEntriesSemaphore,
                _removedEndpointEntries,
                _outputHelper,
                runner,
                timeout);
        }

        public virtual async Task OnBeforeResumeAsync(IEndpointInfo endpointInfo, CancellationToken token)
        {
            if (endpointInfo.RuntimeVersion.Major >= 8 && !string.IsNullOrEmpty(_startupHookPath))
            {
                await ApplyStartupHookAsync(new DiagnosticsClient(endpointInfo.ProcessId), _startupHookPath, token);
            }
        }

        public Task OnAddedEndpointInfoAsync(IEndpointInfo info, CancellationToken token)
        {
            return NotifyCompletionAsync(
                AddOperationName,
                _addedEndpointEntriesSemaphore,
                _addedEndpointEntries,
                _outputHelper,
                info,
                token);
        }

        public Task OnRemovedEndpointInfoAsync(IEndpointInfo info, CancellationToken token)
        {
            return NotifyCompletionAsync(
                RemoveOperationName,
                _removedEndpointEntriesSemaphore,
                _removedEndpointEntries,
                _outputHelper,
                info,
                token);
        }

        private static Task ApplyStartupHookAsync(DiagnosticsClient client, string path, CancellationToken token)
        {
            // DiagnosticsClient.ApplyStartupHookAsync currently is not public
            MethodBase applyStartupHookAsync =
                typeof(DiagnosticsClient).GetMethod(
                    "ApplyStartupHookAsync",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(applyStartupHookAsync);

            return (Task)applyStartupHookAsync.Invoke(client, new object[] { path, token });
        }

        private static async Task<IEndpointInfo> WaitForCompletionAsync(string operation, SemaphoreSlim semaphore, List<CompletionEntry> entries, ITestOutputHelper outputHelper, AppRunner runner, TimeSpan timeout)
        {
            CompletionEntry entry = new(runner);
            using CancellationTokenSource timeoutCancellation = new(timeout);

            await semaphore.WaitAsync(timeoutCancellation.Token);
            try
            {
                entries.Add(entry);
                outputHelper.WriteLine($"[Wait:{operation}] Register App{runner.AppId}");
            }
            finally
            {
                semaphore.Release();
            }

            outputHelper.WriteLine($"[Wait:{operation}] Wait for App{runner.AppId} notification");
            IEndpointInfo endpointInfo = await entry.WithCancellation(timeoutCancellation.Token);
            outputHelper.WriteLine($"[Wait:{operation}] Received App{runner.AppId} notification");

            return endpointInfo;
        }

        private static async Task NotifyCompletionAsync(string operation, SemaphoreSlim semaphore, List<CompletionEntry> entries, ITestOutputHelper outputHelper, IEndpointInfo info, CancellationToken token)
        {
            string endpointOutputString = ToOutputString(info);

            await semaphore.WaitAsync(token);
            try
            {
                outputHelper.WriteLine($"[Source:{operation}] Start notifications for {endpointOutputString}");

                // Create a mapping of the process ID tasks to the completion entries
                IDictionary<Task<int>, CompletionEntry> map = new Dictionary<Task<int>, CompletionEntry>(entries.Count);
                foreach (CompletionEntry entry in entries)
                {
                    map.Add(entry.Runner.ProcessIdTask.WithCancellation(token), entry);
                }

                while (map.Count > 0)
                {
                    // Wait for any of the process ID tasks to complete.
                    Task<int> completedTask = await Task.WhenAny(map.Keys);

                    map.Remove(completedTask, out CompletionEntry entry);

                    outputHelper.WriteLine($"[Source:{operation}] Checking App{entry.Runner.AppId}");

                    if (completedTask.IsCompletedSuccessfully)
                    {
                        // If the process ID matches the one that was reported via the callback,
                        // then signal its completion source.
                        if (info.ProcessId == completedTask.Result)
                        {
                            outputHelper.WriteLine($"[Source:{operation}] Notifying App{entry.Runner.AppId}");
                            entry.Complete(info);

                            entries.Remove(entry);

                            break;
                        }
                    }
                }

                outputHelper.WriteLine($"[Source:{operation}] Finished notifications for {endpointOutputString}");
            }
            finally
            {
                semaphore.Release();
            }
        }

        private static string ToOutputString(IEndpointInfo info)
        {
            return FormattableString.Invariant($"PID={info.ProcessId}, Cookie={info.RuntimeInstanceCookie}");
        }

        private sealed class CompletionEntry
        {
            private readonly TaskCompletionSource<IEndpointInfo> _completionSource =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            public CompletionEntry(AppRunner runner)
            {
                Runner = runner;
            }

            public AppRunner Runner { get; }

            public void Complete(IEndpointInfo endpointInfo)
            {
                _completionSource.TrySetResult(endpointInfo);
            }

            public Task<IEndpointInfo> WithCancellation(CancellationToken token)
            {
                return _completionSource.WithCancellation(token);
            }
        }
    }
}
