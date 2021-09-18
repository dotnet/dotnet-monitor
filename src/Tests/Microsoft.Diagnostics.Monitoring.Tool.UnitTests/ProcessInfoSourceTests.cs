// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public class ProcessInfoSourceTests
    {
        private static readonly TimeSpan DefaultNegativeVerificationTimeout = TimeSpan.FromSeconds(2);

        private readonly ITestOutputHelper _outputHelper;
        private readonly ProcessInfoUtilities _processInfoUtilities;

        public ProcessInfoSourceTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _processInfoUtilities = new ProcessInfoUtilities(_outputHelper);
        }

        /// <summary>
        /// Tests that other <see cref="ServerProcessInfoSource"> methods throw if
        /// <see cref="ServerProcessInfoSource.Start"/> is not called.
        /// </summary>
        [Fact]
        public async Task ServerSourceNoStartTest()
        {
            await using var source = _processInfoUtilities.CreateServerSource(out string transportName);
            // Intentionally do not call Start

            using CancellationTokenSource cancellation = new(DefaultNegativeVerificationTimeout);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => source.GetProcessInfoAsync(cancellation.Token));
        }

        /// <summary>
        /// Tests that the server endpoint info source has not connections if no processes connect to it.
        /// </summary>
        [Fact]
        public async Task ServerSourceNoConnectionsTest()
        {
            await using var source = _processInfoUtilities.CreateServerSource(out _);
            source.Start();

            var processInfos = await _processInfoUtilities.GetProcessInfoAsync(source);
            Assert.Empty(processInfos);
        }

        /// <summary>
        /// Tests that server endpoint info source should throw ObjectDisposedException
        /// from API surface after being disposed.
        /// </summary>
        [Fact]
        public async Task ServerSourceThrowsWhenDisposedTest()
        {
            var source = _processInfoUtilities.CreateServerSource(out _);
            source.Start();

            await source.DisposeAsync();

            // Validate source surface throws after disposal
            Assert.Throws<ObjectDisposedException>(
                () => source.Start());

            Assert.Throws<ObjectDisposedException>(
                () => source.Start(1));

            using CancellationTokenSource cancellation = new(DefaultNegativeVerificationTimeout);
            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => source.GetProcessInfoAsync(cancellation.Token));
        }

        /// <summary>
        /// Tests that server endpoint info source should throw an exception from
        /// <see cref="ServerProcessInfoSource.Start"/> and
        /// <see cref="ServerProcessInfoSource.Start(int)"/> after listening was already started.
        /// </summary>
        [Fact]
        public async Task ServerSourceThrowsWhenMultipleStartTest()
        {
            await using var source = _processInfoUtilities.CreateServerSource(out _);
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
            ProcessInfoSourceCallback callback = new(_outputHelper);
            await using var source = _processInfoUtilities.CreateServerSource(out string transportName, callback);
            source.Start();

            var processInfos = await _processInfoUtilities.GetProcessInfoAsync(source);
            Assert.Empty(processInfos);

            AppRunner runner = _processInfoUtilities.CreateAppRunner(transportName, appTfm);

            Task newProcessInfoTask = callback.WaitForNewProcessInfoAsync(runner, CommonTestTimeouts.StartProcess);

            await runner.ExecuteAsync(async () =>
            {
                await newProcessInfoTask;

                processInfos = await _processInfoUtilities.GetProcessInfoAsync(source);

                var processInfo = Assert.Single(processInfos);
                Assert.NotNull(processInfo.CommandLine);
                Assert.NotNull(processInfo.OperatingSystem);
                Assert.NotNull(processInfo.ProcessArchitecture);
                await ProcessInfoUtilities.VerifyConnectionAsync(runner, processInfo);

                await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
            });

            await Task.Delay(TimeSpan.FromSeconds(1));

            processInfos = await _processInfoUtilities.GetProcessInfoAsync(source);

            Assert.Empty(processInfos);
        }

        /// <summary>
        /// Tests that the server endpoint info source can properly enumerate endpoint infos when multiple
        /// targets connect to it and "disconnect" from it.
        /// </summary>
        [Theory]
        [MemberData(nameof(GetTfmsSupportingPortListener))]
        public async Task ServerSourceAddRemoveMultipleConnectionTest(TargetFrameworkMoniker appTfm)
        {
            ProcessInfoSourceCallback callback = new(_outputHelper);
            await using var source = _processInfoUtilities.CreateServerSource(out string transportName, callback);
            source.Start();

            var processInfos = await _processInfoUtilities.GetProcessInfoAsync(source);
            Assert.Empty(processInfos);

            const int appCount = 5;
            AppRunner[] runners = new AppRunner[appCount];
            Task[] newProcessInfoTasks = new Task[appCount];

            // Start all app instances
            for (int i = 0; i < appCount; i++)
            {
                runners[i] = _processInfoUtilities.CreateAppRunner(transportName, appTfm, appId: i + 1);
                newProcessInfoTasks[i] = callback.WaitForNewProcessInfoAsync(runners[i], CommonTestTimeouts.StartProcess);
            }

            await runners.ExecuteAsync(async () =>
            {
                _outputHelper.WriteLine("Waiting for all new endpoint info notifications.");
                await Task.WhenAll(newProcessInfoTasks);
                _outputHelper.WriteLine("Received all new endpoint info notifications.");

                processInfos = await _processInfoUtilities.GetProcessInfoAsync(source);

                Assert.Equal(appCount, processInfos.Count());

                for (int i = 0; i < appCount; i++)
                {
                    int processId = await runners[i].ProcessIdTask;

                    IProcessInfo processInfo = processInfos.FirstOrDefault(info => info.ProcessId == processId);
                    Assert.NotNull(processInfo);
                    Assert.NotNull(processInfo.CommandLine);
                    Assert.NotNull(processInfo.OperatingSystem);
                    Assert.NotNull(processInfo.ProcessArchitecture);

                    await ProcessInfoUtilities.VerifyConnectionAsync(runners[i], processInfo);

                    await runners[i].SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                }
            });

            for (int i = 0; i < appCount; i++)
            {
                Assert.True(0 == runners[i].ExitCode, $"App {i} exit code is non-zero.");
            }

            await Task.Delay(TimeSpan.FromSeconds(1));

            processInfos = await _processInfoUtilities.GetProcessInfoAsync(source);

            Assert.Empty(processInfos);
        }

        public static IEnumerable<object[]> GetTfmsSupportingPortListener()
        {
            yield return new object[] { TargetFrameworkMoniker.Net50 };
            yield return new object[] { TargetFrameworkMoniker.Net60 };
        }
    }
}