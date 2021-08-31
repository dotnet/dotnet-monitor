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
    public class EndpointInfoSourceTests
    {
        private static readonly TimeSpan DefaultNegativeVerificationTimeout = TimeSpan.FromSeconds(2);

        private static readonly TimeSpan GetEndpointInfoTimeout = TimeSpan.FromSeconds(10);

        private readonly ITestOutputHelper _outputHelper;

        public EndpointInfoSourceTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Tests that other <see cref="ServerEndpointInfoSource"> methods throw if
        /// <see cref="ServerEndpointInfoSource.Start"/> is not called.
        /// </summary>
        [Fact]
        public async Task ServerSourceNoStartTest()
        {
            await using var source = CreateServerSource(out string transportName);
            // Intentionally do not call Start

            using CancellationTokenSource cancellation = new(DefaultNegativeVerificationTimeout);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => source.GetEndpointInfoAsync(cancellation.Token));
        }

        /// <summary>
        /// Tests that the server endpoint info source has not connections if no processes connect to it.
        /// </summary>
        [Fact]
        public async Task ServerSourceNoConnectionsTest()
        {
            await using var source = CreateServerSource(out _);
            source.Start();

            var endpointInfos = await GetEndpointInfoAsync(source);
            Assert.Empty(endpointInfos);
        }

        /// <summary>
        /// Tests that server endpoint info source should throw ObjectDisposedException
        /// from API surface after being disposed.
        /// </summary>
        [Fact]
        public async Task ServerSourceThrowsWhenDisposedTest()
        {
            var source = CreateServerSource(out _);
            source.Start();

            await source.DisposeAsync();

            // Validate source surface throws after disposal
            Assert.Throws<ObjectDisposedException>(
                () => source.Start());

            Assert.Throws<ObjectDisposedException>(
                () => source.Start(1));

            using CancellationTokenSource cancellation = new(DefaultNegativeVerificationTimeout);
            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => source.GetEndpointInfoAsync(cancellation.Token));
        }

        /// <summary>
        /// Tests that server endpoint info source should throw an exception from
        /// <see cref="ServerEndpointInfoSource.Start"/> and
        /// <see cref="ServerEndpointInfoSource.Start(int)"/> after listening was already started.
        /// </summary>
        [Fact]
        public async Task ServerSourceThrowsWhenMultipleStartTest()
        {
            await using var source = CreateServerSource(out _);
            source.Start();

            Assert.Throws<InvalidOperationException>(
                () => source.Start());

            Assert.Throws<InvalidOperationException>(
                () => source.Start(1));
        }

        /// <summary>
        /// Tests that the server endpoint info source can properly enumerate endpoint infos when a single
        /// target connects to it and "disconnects" from it.
        /// </summary>
        [Theory]
        [MemberData(nameof(GetTfmsSupportingPortListener))]
        public async Task ServerSourceAddRemoveSingleConnectionTest(TargetFrameworkMoniker appTfm)
        {
            ServerEndpointInfoCallback callback = new(_outputHelper);
            await using var source = CreateServerSource(out string transportName, callback);
            source.Start();

            var endpointInfos = await GetEndpointInfoAsync(source);
            Assert.Empty(endpointInfos);

            AppRunner runner = CreateAppRunner(transportName, appTfm);

            using CancellationTokenSource cancellation = new(CommonTestTimeouts.StartProcess);
            Task newEndpointInfoTask = callback.WaitForNewEndpointInfoAsync(runner, cancellation.Token);

            await runner.ExecuteAsync(async () =>
            {
                await newEndpointInfoTask;

                endpointInfos = await GetEndpointInfoAsync(source);

                var endpointInfo = Assert.Single(endpointInfos);
                Assert.NotNull(endpointInfo.CommandLine);
                Assert.NotNull(endpointInfo.OperatingSystem);
                Assert.NotNull(endpointInfo.ProcessArchitecture);
                VerifyConnection(runner, endpointInfo);

                await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
            });

            await Task.Delay(TimeSpan.FromSeconds(1));

            endpointInfos = await GetEndpointInfoAsync(source);

            Assert.Empty(endpointInfos);
        }

        /// <summary>
        /// Tests that the server endpoint info source can properly enumerate endpoint infos when multiple
        /// targets connect to it and "disconnect" from it.
        /// </summary>
        [Theory]
        [MemberData(nameof(GetTfmsSupportingPortListener))]
        public async Task ServerSourceAddRemoveMultipleConnectionTest(TargetFrameworkMoniker appTfm)
        {
            ServerEndpointInfoCallback callback = new(_outputHelper);
            await using var source = CreateServerSource(out string transportName, callback);
            source.Start();

            var endpointInfos = await GetEndpointInfoAsync(source);
            Assert.Empty(endpointInfos);

            const int appCount = 5;
            AppRunner[] runners = new AppRunner[appCount];
            Task[] newEndpointInfoTasks = new Task[appCount];

            // Start all app instances
            using CancellationTokenSource cancellation = new(CommonTestTimeouts.StartProcess);
            for (int i = 0; i < appCount; i++)
            {
                runners[i] = CreateAppRunner(transportName, appTfm, appId: i + 1);
                newEndpointInfoTasks[i] = callback.WaitForNewEndpointInfoAsync(runners[i], cancellation.Token);
            }

            await runners.ExecuteAsync(async () =>
            {
                _outputHelper.WriteLine("Waiting for all new endpoint info notifications.");
                await Task.WhenAll(newEndpointInfoTasks);
                _outputHelper.WriteLine("Received all new endpoint info notifications.");

                endpointInfos = await GetEndpointInfoAsync(source);

                Assert.Equal(appCount, endpointInfos.Count());

                for (int i = 0; i < appCount; i++)
                {
                    IEndpointInfo endpointInfo = endpointInfos.FirstOrDefault(info => info.ProcessId == runners[i].ProcessId);
                    Assert.NotNull(endpointInfo);
                    Assert.NotNull(endpointInfo.CommandLine);
                    Assert.NotNull(endpointInfo.OperatingSystem);
                    Assert.NotNull(endpointInfo.ProcessArchitecture);

                    VerifyConnection(runners[i], endpointInfo);

                    await runners[i].SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                }
            });

            for (int i = 0; i < appCount; i++)
            {
                Assert.True(0 == runners[i].ExitCode, $"App {i} exit code is non-zero.");
            }

            await Task.Delay(TimeSpan.FromSeconds(1));

            endpointInfos = await GetEndpointInfoAsync(source);

            Assert.Empty(endpointInfos);
        }

        public static IEnumerable<object[]> GetTfmsSupportingPortListener()
        {
            yield return new object[] { TargetFrameworkMoniker.Net50 };
            yield return new object[] { TargetFrameworkMoniker.Net60 };
        }

        private ServerEndpointInfoSource CreateServerSource(out string transportName, ServerEndpointInfoCallback callback = null)
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

        private AppRunner CreateAppRunner(string transportName, TargetFrameworkMoniker tfm, int appId = 1)
        {
            AppRunner appRunner = new(_outputHelper, Assembly.GetExecutingAssembly(), appId, tfm);
            appRunner.ConnectionMode = DiagnosticPortConnectionMode.Connect;
            appRunner.DiagnosticPortPath = transportName;
            appRunner.ScenarioName = TestAppScenarios.AsyncWait.Name;
            return appRunner;
        }

        private async Task<IEnumerable<IEndpointInfo>> GetEndpointInfoAsync(ServerEndpointInfoSource source)
        {
            _outputHelper.WriteLine("Getting endpoint infos.");
            using CancellationTokenSource cancellationSource = new(GetEndpointInfoTimeout);
            return await source.GetEndpointInfoAsync(cancellationSource.Token);
        }

        /// <summary>
        /// Verifies basic information on the connection and that it matches the target process from the runner.
        /// </summary>
        private static void VerifyConnection(AppRunner runner, IEndpointInfo endpointInfo)
        {
            Assert.NotNull(runner);
            Assert.NotNull(endpointInfo);
            Assert.Equal(runner.ProcessId, endpointInfo.ProcessId);
            Assert.NotEqual(Guid.Empty, endpointInfo.RuntimeInstanceCookie);
            Assert.NotNull(endpointInfo.Endpoint);
        }

        private sealed class ServerEndpointInfoCallback : IEndpointInfoSourceCallbacks
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
