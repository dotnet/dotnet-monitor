// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    internal class EndpointUtilities
    {
        private readonly ITestOutputHelper _outputHelper;

        private static readonly TimeSpan GetEndpointInfoTimeout = TimeSpan.FromSeconds(10);

        public EndpointUtilities(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        public ServerEndpointInfoSource CreateServerSource(out string transportName, ServerEndpointInfoCallback callback = null)
        {
            DiagnosticPortHelper.Generate(DiagnosticPortConnectionMode.Listen, out _, out transportName);
            _outputHelper.WriteLine("Starting server endpoint info source at '" + transportName + "'.");

            List<IEndpointInfoSourceCallbacks> callbacks = new();
            if (null != callback)
            {
                callbacks.Add(callback);
            }
            return new ServerEndpointInfoSource(transportName, callbacks);
        }

        public AppRunner CreateAppRunner(string transportName, TargetFrameworkMoniker tfm, int appId = 1)
        {
            AppRunner appRunner = new(_outputHelper, Assembly.GetExecutingAssembly(), appId, tfm);
            appRunner.ConnectionMode = DiagnosticPortConnectionMode.Connect;
            appRunner.DiagnosticPortPath = transportName;
            appRunner.ScenarioName = TestAppScenarios.AsyncWait.Name;
            return appRunner;
        }

        public async Task<IEnumerable<IEndpointInfo>> GetEndpointInfoAsync(ServerEndpointInfoSource source)
        {
            _outputHelper.WriteLine("Getting endpoint infos.");
            using CancellationTokenSource cancellationSource = new(GetEndpointInfoTimeout);
            return await source.GetEndpointInfoAsync(cancellationSource.Token);
        }

        /// <summary>
        /// Verifies basic information on the connection and that it matches the target process from the runner.
        /// </summary>
        public static async Task VerifyConnectionAsync(AppRunner runner, IEndpointInfo endpointInfo)
        {
            Assert.NotNull(runner);
            Assert.NotNull(endpointInfo);
            Assert.Equal(await runner.ProcessIdTask, endpointInfo.ProcessId);
            Assert.NotEqual(Guid.Empty, endpointInfo.RuntimeInstanceCookie);
            Assert.NotNull(endpointInfo.Endpoint);
        }

        public sealed class ServerEndpointInfoCallback : IEndpointInfoSourceCallbacks
        {
            private readonly ITestOutputHelper _outputHelper;
            /// <summary>
            /// Use to protect the completion list from mutation while processing
            /// callbacks from it. The processing is done in an async method with async
            /// calls, which are not allowed in a lock, thus use SemaphoreSlim.
            /// </summary>
            private readonly SemaphoreSlim _completionEntriesSemaphore = new(1);
            private readonly List<CompletionEntry> _completionEntries = new();

            public ServerEndpointInfoCallback(ITestOutputHelper outputHelper)
            {
                _outputHelper = outputHelper;
            }

            public async Task<IEndpointInfo> WaitForNewEndpointInfoAsync(AppRunner runner, CancellationToken token)
            {
                CompletionEntry entry = new(runner);
                using var _ = token.Register(() => entry.CompletionSource.TrySetCanceled(token));

                await _completionEntriesSemaphore.WaitAsync(token);
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
                IEndpointInfo endpointInfo = await entry.CompletionSource.Task;
                _outputHelper.WriteLine($"[Wait] Received App{runner.AppId} notification");

                return endpointInfo;
            }

            public Task OnBeforeResumeAsync(IEndpointInfo endpointInfo, CancellationToken token)
            {
                return Task.CompletedTask;
            }

            public async Task OnAddedEndpointInfoAsync(IEndpointInfo info, CancellationToken token)
            {
                _outputHelper.WriteLine($"[Source] Added: {ToOutputString(info)}");

                await _completionEntriesSemaphore.WaitAsync(token);
                try
                {
                    _outputHelper.WriteLine($"[Source] Start notifications for process {info.ProcessId}");

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
                            if (info.ProcessId == completedTask.Result)
                            {
                                _outputHelper.WriteLine($"[Source] Notifying App{entry.Runner.AppId}");
                                entry.CompletionSource.TrySetResult(info);

                                _completionEntries.Remove(entry);

                                break;
                            }
                        }
                    }

                    _outputHelper.WriteLine($"[Source] Finished notifications for process {info.ProcessId}");
                }
                finally
                {
                    _completionEntriesSemaphore.Release();
                }
            }

            public void OnRemovedEndpointInfo(IEndpointInfo info)
            {
                _outputHelper.WriteLine($"[Source] Removed: {ToOutputString(info)}");
            }

            private static string ToOutputString(IEndpointInfo info)
            {
                return FormattableString.Invariant($"PID={info.ProcessId}, Cookie={info.RuntimeInstanceCookie}");
            }

            private sealed class CompletionEntry
            {
                public CompletionEntry(AppRunner runner)
                {
                    Runner = runner;
                    CompletionSource = new TaskCompletionSource<IEndpointInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
                }

                public AppRunner Runner { get; }

                public TaskCompletionSource<IEndpointInfo> CompletionSource { get; }
            }
        }
    }
}
