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
using static Microsoft.Diagnostics.Monitoring.Tool.UnitTests.EndpointUtilities;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public class EndpointInfoSourceTests
    {
        private static readonly TimeSpan DefaultNegativeVerificationTimeout = TimeSpan.FromSeconds(2);

        private readonly ITestOutputHelper _outputHelper;
        private readonly EndpointUtilities _endpointUtilities;

        public EndpointInfoSourceTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _endpointUtilities = new EndpointUtilities(_outputHelper);
        }

        /// <summary>
        /// Tests that other <see cref="ServerEndpointInfoSource"> methods throw if
        /// <see cref="ServerEndpointInfoSource.Start"/> is not called.
        /// </summary>
        [Fact]
        public async Task ServerSourceNoStartTest()
        {
            await using var source = _endpointUtilities.CreateServerSource(out string transportName);
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
            await using var source = _endpointUtilities.CreateServerSource(out _);
            source.Start();

            var endpointInfos = await _endpointUtilities.GetEndpointInfoAsync(source);
            Assert.Empty(endpointInfos);
        }

        /// <summary>
        /// Tests that server endpoint info source should throw ObjectDisposedException
        /// from API surface after being disposed.
        /// </summary>
        [Fact]
        public async Task ServerSourceThrowsWhenDisposedTest()
        {
            var source = _endpointUtilities.CreateServerSource(out _);
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
            await using var source = _endpointUtilities.CreateServerSource(out _);
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
            EndpointInfoSourceCallback callback = new(_outputHelper);
            await using var source = _endpointUtilities.CreateServerSource(out string transportName, callback);
            source.Start();

            var endpointInfos = await _endpointUtilities.GetEndpointInfoAsync(source);
            Assert.Empty(endpointInfos);

            AppRunner runner = _endpointUtilities.CreateAppRunner(transportName, appTfm);

            Task newEndpointInfoTask = callback.WaitForNewEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);

            await runner.ExecuteAsync(async () =>
            {
                await newEndpointInfoTask;

                endpointInfos = await _endpointUtilities.GetEndpointInfoAsync(source);

                var endpointInfo = Assert.Single(endpointInfos);
                Assert.NotNull(endpointInfo.CommandLine);
                Assert.NotNull(endpointInfo.OperatingSystem);
                Assert.NotNull(endpointInfo.ProcessArchitecture);
                await VerifyConnectionAsync(runner, endpointInfo);

                await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
            });

            await Task.Delay(TimeSpan.FromSeconds(1));

            endpointInfos = await _endpointUtilities.GetEndpointInfoAsync(source);

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
            EndpointInfoSourceCallback callback = new(_outputHelper);
            await using var source = _endpointUtilities.CreateServerSource(out string transportName, callback);
            source.Start();

            var endpointInfos = await _endpointUtilities.GetEndpointInfoAsync(source);
            Assert.Empty(endpointInfos);

            const int appCount = 5;
            AppRunner[] runners = new AppRunner[appCount];
            Task[] newEndpointInfoTasks = new Task[appCount];

            // Start all app instances
            for (int i = 0; i < appCount; i++)
            {
                runners[i] = _endpointUtilities.CreateAppRunner(transportName, appTfm, appId: i + 1);
                newEndpointInfoTasks[i] = callback.WaitForNewEndpointInfoAsync(runners[i], CommonTestTimeouts.StartProcess);
            }

            await runners.ExecuteAsync(async () =>
            {
                _outputHelper.WriteLine("Waiting for all new endpoint info notifications.");
                await Task.WhenAll(newEndpointInfoTasks);
                _outputHelper.WriteLine("Received all new endpoint info notifications.");

                endpointInfos = await _endpointUtilities.GetEndpointInfoAsync(source);

                Assert.Equal(appCount, endpointInfos.Count());

                for (int i = 0; i < appCount; i++)
                {
                    int processId = await runners[i].ProcessIdTask;

                    IEndpointInfo endpointInfo = endpointInfos.FirstOrDefault(info => info.ProcessId == processId);
                    Assert.NotNull(endpointInfo);
                    Assert.NotNull(endpointInfo.CommandLine);
                    Assert.NotNull(endpointInfo.OperatingSystem);
                    Assert.NotNull(endpointInfo.ProcessArchitecture);

                    await VerifyConnectionAsync(runners[i], endpointInfo);

                    await runners[i].SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                }
            });

            for (int i = 0; i < appCount; i++)
            {
                Assert.True(0 == runners[i].ExitCode, $"App {i} exit code is non-zero.");
            }

            await Task.Delay(TimeSpan.FromSeconds(1));

            endpointInfos = await _endpointUtilities.GetEndpointInfoAsync(source);

            Assert.Empty(endpointInfos);
        }

        public static IEnumerable<object[]> GetTfmsSupportingPortListener()
        {
            yield return new object[] { TargetFrameworkMoniker.Net50 };
            yield return new object[] { TargetFrameworkMoniker.Net60 };
        }
    }
}