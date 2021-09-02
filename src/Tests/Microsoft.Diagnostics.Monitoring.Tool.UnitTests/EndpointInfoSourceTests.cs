﻿// Licensed to the .NET Foundation under one or more agreements.
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

            Task newEndpointInfoTask = callback.WaitForNewEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);

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
            for (int i = 0; i < appCount; i++)
            {
                runners[i] = CreateAppRunner(transportName, appTfm, appId: i + 1);
                newEndpointInfoTasks[i] = callback.WaitForNewEndpointInfoAsync(runners[i], CommonTestTimeouts.StartProcess);
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

        internal ServerEndpointInfoSource CreateServerSource(out string transportName, ServerEndpointInfoCallback callback = null)
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

        internal AppRunner CreateAppRunner(string transportName, TargetFrameworkMoniker tfm, int appId = 1)
        {
            AppRunner appRunner = new(_outputHelper, Assembly.GetExecutingAssembly(), appId, tfm);
            appRunner.ConnectionMode = DiagnosticPortConnectionMode.Connect;
            appRunner.DiagnosticPortPath = transportName;
            appRunner.ScenarioName = TestAppScenarios.AsyncWait.Name;
            return appRunner;
        }

        internal async Task<IEnumerable<IEndpointInfo>> GetEndpointInfoAsync(ServerEndpointInfoSource source)
        {
            _outputHelper.WriteLine("Getting endpoint infos.");
            using CancellationTokenSource cancellationSource = new(GetEndpointInfoTimeout);
            return await source.GetEndpointInfoAsync(cancellationSource.Token);
        }

        /// <summary>
        /// Verifies basic information on the connection and that it matches the target process from the runner.
        /// </summary>
        internal static void VerifyConnection(AppRunner runner, IEndpointInfo endpointInfo)
        {
            Assert.NotNull(runner);
            Assert.NotNull(endpointInfo);
            Assert.Equal(runner.ProcessId, endpointInfo.ProcessId);
            Assert.NotEqual(Guid.Empty, endpointInfo.RuntimeInstanceCookie);
            Assert.NotNull(endpointInfo.Endpoint);
        }

        internal sealed class ServerEndpointInfoCallback : IEndpointInfoSourceCallbacks
        {
            private readonly ITestOutputHelper _outputHelper;
            private readonly List<(AppRunner Runner, TaskCompletionSource<IEndpointInfo> CompletionSource)> _addedEndpointInfoSources = new();

            public ServerEndpointInfoCallback(ITestOutputHelper outputHelper)
            {
                _outputHelper = outputHelper;
            }

            public async Task<IEndpointInfo> WaitForNewEndpointInfoAsync(AppRunner runner, TimeSpan timeout)
            {
                TaskCompletionSource<IEndpointInfo> addedEndpointInfoSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
                using CancellationTokenSource timeoutCancellation = new();
                var token = timeoutCancellation.Token;
                using var _ = token.Register(() => addedEndpointInfoSource.TrySetCanceled(token));

                lock (_addedEndpointInfoSources)
                {
                    _addedEndpointInfoSources.Add(new(runner, addedEndpointInfoSource));
                    _outputHelper.WriteLine($"[Wait] Register App{runner.AppId}");
                }

                _outputHelper.WriteLine($"[Wait] Wait for App{runner.AppId} notification");
                timeoutCancellation.CancelAfter(timeout);
                IEndpointInfo endpointInfo = await addedEndpointInfoSource.Task;
                _outputHelper.WriteLine($"[Wait] Received App{runner.AppId} notification");

                return endpointInfo;
            }

            public Task OnBeforeResumeAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public void OnAddedEndpointInfo(IEndpointInfo info)
            {
                _outputHelper.WriteLine($"[Source] Added: {ToOutputString(info)}");

                lock (_addedEndpointInfoSources)
                {
                    _outputHelper.WriteLine($"[Source] Start notifications for process {info.ProcessId}");

                    foreach (var sourceTuple in _addedEndpointInfoSources.ToList())
                    {
                        AppRunner runner = sourceTuple.Runner;
                        _outputHelper.WriteLine($"[Source] Checking App{runner.AppId}");
                        try
                        {
                            if (info.ProcessId == runner.ProcessId)
                            {
                                _outputHelper.WriteLine($"[Source] Notifying App{runner.AppId}");
                                sourceTuple.CompletionSource.TrySetResult(info);
                                _addedEndpointInfoSources.Remove(sourceTuple);
                                break;
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            // Thrown if app runner hasn't started process yet.
                            _outputHelper.WriteLine($"[Source] App{runner.AppId} has not start yet.");
                        }
                    }

                    _outputHelper.WriteLine($"[Source] Finished notifications for process {info.ProcessId}");
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
        }
    }
}