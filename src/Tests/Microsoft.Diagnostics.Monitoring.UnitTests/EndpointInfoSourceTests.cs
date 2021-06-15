// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.UnitTests.Runners;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.UnitTests
{
#if NET5_0_OR_GREATER
    public class EndpointInfoSourceTests
    {
        private static readonly TimeSpan DefaultNegativeVerificationTimeout = TimeSpan.FromSeconds(2);

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
        [Fact]
        public async Task ServerSourceAddRemoveSingleConnectionTest()
        {
            await using var source = CreateServerSource(out string transportName);
            source.Start();

            var endpointInfos = await GetEndpointInfoAsync(source);
            Assert.Empty(endpointInfos);

            AppRunner runner = CreateAppRunner(transportName);

            Task newEndpointInfoTask = source.WaitForNewEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);

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
        [Fact]
        public async Task ServerSourceAddRemoveMultipleConnectionTest()
        {
            await using var source = CreateServerSource(out string transportName);
            source.Start();

            var endpointInfos = await GetEndpointInfoAsync(source);
            Assert.Empty(endpointInfos);

            const int appCount = 5;
            AppRunner[] runners = new AppRunner[appCount];
            Task[] newEndpointInfoTasks = new Task[appCount];

            // Start all app instances
            for (int i = 0; i < appCount; i++)
            {
                runners[i] = CreateAppRunner(transportName, appId: i + 1);
                newEndpointInfoTasks[i] = source.WaitForNewEndpointInfoAsync(runners[i], CommonTestTimeouts.StartProcess);
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

        private TestServerEndpointInfoSource CreateServerSource(out string transportName)
        {
            DiagnosticPortHelper.Generate(Options.DiagnosticPortConnectionMode.Listen, out _, out transportName);
            _outputHelper.WriteLine("Starting server endpoint info source at '" + transportName + "'.");
            return new TestServerEndpointInfoSource(transportName, _outputHelper);
        }

        private AppRunner CreateAppRunner(string transportName = null, int appId = 1)
        {
            AppRunner appRunner = new(_outputHelper, Assembly.GetExecutingAssembly(), appId);
            appRunner.ConnectionMode = Options.DiagnosticPortConnectionMode.Connect;
            appRunner.DiagnosticPortPath = transportName;
            appRunner.ScenarioName = TestAppScenarios.AsyncWait.Name;
            return appRunner;
        }

        private async Task<IEnumerable<IEndpointInfo>> GetEndpointInfoAsync(ServerEndpointInfoSource source)
        {
            _outputHelper.WriteLine("Getting endpoint infos.");
            using CancellationTokenSource cancellationSource = new(TimeSpan.FromSeconds(10));
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

        private sealed class TestServerEndpointInfoSource : ServerEndpointInfoSource
        {
            private readonly ITestOutputHelper _outputHelper;
            private readonly List<Tuple<AppRunner, TaskCompletionSource<EndpointInfo>>> _addedEndpointInfoSources = new();

            public TestServerEndpointInfoSource(string transportPath, ITestOutputHelper outputHelper)
                : base(transportPath)
            {
                _outputHelper = outputHelper;
            }

            public async Task<EndpointInfo> WaitForNewEndpointInfoAsync(AppRunner runner, TimeSpan timeout)
            {
                TaskCompletionSource<EndpointInfo> addedEndpointInfoSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
                using CancellationTokenSource timeoutCancellation = new();
                var token = timeoutCancellation.Token;
                using var _ = token.Register(() => addedEndpointInfoSource.TrySetCanceled(token));

                lock (_addedEndpointInfoSources)
                {
                    _addedEndpointInfoSources.Add(Tuple.Create(runner, addedEndpointInfoSource));
                }

                _outputHelper.WriteLine("Waiting for new endpoint info.");
                timeoutCancellation.CancelAfter(timeout);
                EndpointInfo endpointInfo = await addedEndpointInfoSource.Task;
                _outputHelper.WriteLine("Notified of new endpoint info.");

                return endpointInfo;
            }

            internal override void OnAddedEndpointInfo(EndpointInfo info)
            {
                _outputHelper.WriteLine($"Added endpoint info to collection: {info.DebuggerDisplay}");
                
                lock (_addedEndpointInfoSources)
                {
                    foreach (var source in _addedEndpointInfoSources)
                    {
                        try
                        {
                            if (info.ProcessId == source.Item1.ProcessId)
                            {
                                _outputHelper.WriteLine($"Notifying endpoint registration for process ID {info.ProcessId}");
                                source.Item2.TrySetResult(info);
                                _addedEndpointInfoSources.Remove(source);
                                break;
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            // Thrown in app runner hasn't started process yet.
                        }
                    }
                }
            }

            internal override void OnRemovedEndpointInfo(EndpointInfo info)
            {
                _outputHelper.WriteLine($"Removed endpoint info from collection: {info.DebuggerDisplay}");
            }
        }
    }
#endif
}
