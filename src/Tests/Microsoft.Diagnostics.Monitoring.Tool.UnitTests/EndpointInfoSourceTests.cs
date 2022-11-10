// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class EndpointInfoSourceTests
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly EndpointUtilities _endpointUtilities;

        public EndpointInfoSourceTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _endpointUtilities = new EndpointUtilities(_outputHelper);
        }

        /// <summary>
        /// Tests that the server endpoint info source has not connections if no processes connect to it.
        /// </summary>
        [Fact]
        public async Task ServerSourceNoConnectionsTest()
        {
            await using ServerSourceHolder sourceHolder = await _endpointUtilities.StartServerAsync();

            var endpointInfos = await _endpointUtilities.GetEndpointInfoAsync(sourceHolder.Source);
            Assert.Empty(endpointInfos);
        }

        /// <summary>
        /// Tests that the server endpoint info source can properly enumerate endpoint infos when a single
        /// target connects to it and "disconnects" from it.
        /// </summary>
        [Theory]
        [MemberData(nameof(ActionTestsHelper.GetTfms), MemberType = typeof(ActionTestsHelper))]
        public async Task ServerSourceAddRemoveSingleConnectionTest(TargetFrameworkMoniker appTfm)
        {
            EndpointInfoSourceCallback callback = new(_outputHelper);
            await using ServerSourceHolder sourceHolder = await _endpointUtilities.StartServerAsync(callback);

            var endpointInfos = await _endpointUtilities.GetEndpointInfoAsync(sourceHolder.Source);
            Assert.Empty(endpointInfos);

            await using AppRunner runner = _endpointUtilities.CreateAppRunner(Assembly.GetExecutingAssembly(), sourceHolder.TransportName, appTfm);

            Task addedEndpointTask = callback.WaitAddedEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);
            Task removedEndpointTask = callback.WaitRemovedEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);

            await runner.ExecuteAsync(async () =>
            {
                _outputHelper.WriteLine("Waiting for added endpoint notification.");
                await addedEndpointTask;
                _outputHelper.WriteLine("Received added endpoint notifications.");

                endpointInfos = await _endpointUtilities.GetEndpointInfoAsync(sourceHolder.Source);

                var endpointInfo = Assert.Single(endpointInfos);

                ValidateEndpointInfo(endpointInfo);

                await EndpointUtilities.VerifyConnectionAsync(runner, endpointInfo);

                await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
            });

            _outputHelper.WriteLine("Waiting for removed endpoint notification.");
            await removedEndpointTask;
            _outputHelper.WriteLine("Received removed endpoint notifications.");

            endpointInfos = await _endpointUtilities.GetEndpointInfoAsync(sourceHolder.Source);

            Assert.Empty(endpointInfos);
        }

        /// <summary>
        /// Tests that the server endpoint info source can properly enumerate endpoint infos when multiple
        /// targets connect to it and "disconnect" from it.
        /// </summary>
        [Theory]
        [MemberData(nameof(ActionTestsHelper.GetTfms), MemberType = typeof(ActionTestsHelper))]
        public async Task ServerSourceAddRemoveMultipleConnectionTest(TargetFrameworkMoniker appTfm)
        {
            EndpointInfoSourceCallback callback = new(_outputHelper);
            await using ServerSourceHolder sourceHolder = await _endpointUtilities.StartServerAsync(callback);

            var endpointInfos = await _endpointUtilities.GetEndpointInfoAsync(sourceHolder.Source);
            Assert.Empty(endpointInfos);

            const int appCount = 5;
            AppRunner[] runners = new AppRunner[appCount];
            Task[] addedEndpointTasks = new Task[appCount];
            Task[] removedEndpointTasks = new Task[appCount];

            // Start all app instances
            for (int i = 0; i < appCount; i++)
            {
                runners[i] = _endpointUtilities.CreateAppRunner(Assembly.GetExecutingAssembly(), sourceHolder.TransportName, appTfm, appId: i + 1);
                addedEndpointTasks[i] = callback.WaitAddedEndpointInfoAsync(runners[i], CommonTestTimeouts.StartProcess);
                removedEndpointTasks[i] = callback.WaitRemovedEndpointInfoAsync(runners[i], CommonTestTimeouts.StartProcess);
            }

            await using IAsyncDisposable _ = runners.CreateItemDisposer();

            await runners.ExecuteAsync(async () =>
            {
                _outputHelper.WriteLine("Waiting for all added endpoint notifications.");
                await Task.WhenAll(addedEndpointTasks);
                _outputHelper.WriteLine("Received all added endpoint notifications.");

                endpointInfos = await _endpointUtilities.GetEndpointInfoAsync(sourceHolder.Source);

                Assert.Equal(appCount, endpointInfos.Count());

                for (int i = 0; i < appCount; i++)
                {
                    int processId = await runners[i].ProcessIdTask;

                    IEndpointInfo endpointInfo = endpointInfos.FirstOrDefault(info => info.ProcessId == processId);

                    ValidateEndpointInfo(endpointInfo);

                    await EndpointUtilities.VerifyConnectionAsync(runners[i], endpointInfo);

                    await runners[i].SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                }
            });

            _outputHelper.WriteLine("Waiting for all removed endpoint notifications.");
            await Task.WhenAll(removedEndpointTasks);
            _outputHelper.WriteLine("Received all removed endpoint notifications.");

            for (int i = 0; i < appCount; i++)
            {
                Assert.True(0 == runners[i].ExitCode, $"App {i} exit code is non-zero.");
            }

            await Task.Delay(TimeSpan.FromSeconds(1));

            endpointInfos = await _endpointUtilities.GetEndpointInfoAsync(sourceHolder.Source);

            Assert.Empty(endpointInfos);
        }

        /// <summary>
        /// Tests that the server endpoint info source will not prune a process while a simulated
        /// dump operation is in progress.
        /// </summary>
        [Theory]
        [MemberData(nameof(ActionTestsHelper.GetTfms), MemberType = typeof(ActionTestsHelper))]
        public async Task ServerSourceNoPruneDuringDumpTest(TargetFrameworkMoniker appTfm)
        {
            EndpointInfoSourceCallback callback = new(_outputHelper);
            var operationTrackerService = new OperationTrackerService();
            MockDumpService dumpService = new(operationTrackerService);
            await using ServerSourceHolder sourceHolder = await _endpointUtilities.StartServerAsync(callback, dumpService, operationTrackerService);

            await using AppRunner runner = _endpointUtilities.CreateAppRunner(Assembly.GetExecutingAssembly(), sourceHolder.TransportName, appTfm);

            Task<IEndpointInfo> addedEndpointTask = callback.WaitAddedEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);
            Task<IEndpointInfo> removedEndpointTask = callback.WaitRemovedEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);

            Task<Stream> dumpTask = null;
            IEndpointInfo endpointInfo = null;

            await runner.ExecuteAsync(async () =>
            {
                _outputHelper.WriteLine("Waiting for added endpoint notification.");
                endpointInfo = await addedEndpointTask;
                _outputHelper.WriteLine("Received added endpoint notifications.");

                // Start a dump operation; the process should not be pruned until the operation is completed.
                dumpTask = dumpService.DumpAsync(endpointInfo, DumpType.Triage, CancellationToken.None);

                await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
            });

            // At this point, the process should no longer exist; but since the mock dump operation is
            // in progress, the ServiceEndpointInfoSource should not prune the process until the operation
            // is complete.

            int processId = await runner.ProcessIdTask;

            // Test that the process still exists.
            Assert.False(removedEndpointTask.IsCompleted);
            IEnumerable<IEndpointInfo> endpointInfos = await _endpointUtilities.GetEndpointInfoAsync(sourceHolder.Source);
            endpointInfo = Assert.Single(endpointInfos);
            Assert.Equal(processId, endpointInfo.ProcessId);

            // Signal and wait for mock dump operation to complete.
            dumpService.CompleteOperation();
            await dumpTask;

            // Wait for process removal notification; this may take a few seconds for process pruning to occur
            endpointInfo = await removedEndpointTask;
            Assert.Equal(processId, endpointInfo.ProcessId);

            // Test that process should no longer exist
            endpointInfos = await _endpointUtilities.GetEndpointInfoAsync(sourceHolder.Source);
            Assert.Empty(endpointInfos);
        }

        private static void ValidateEndpointInfo(IEndpointInfo endpointInfo)
        {
            Assert.NotNull(endpointInfo);
            Assert.NotNull(endpointInfo.CommandLine);
            Assert.NotNull(endpointInfo.OperatingSystem);
            Assert.NotNull(endpointInfo.ProcessArchitecture);
        }

        /// <summary>
        /// <see cref="IDumpService"/> implementation that simulates a dump operation. Allows for controlled
        /// start and completion of a dump operation on a single process.
        /// </summary>
        private sealed class MockDumpService : IDumpService
        {
            private readonly TaskCompletionSource<Stream> _dumpCompletionSource =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            private readonly OperationTrackerService _operationTrackerService;

            public MockDumpService(OperationTrackerService operationTrackerService)
            {
                _operationTrackerService = operationTrackerService;
            }

            public async Task<Stream> DumpAsync(IEndpointInfo endpointInfo, DumpType mode, CancellationToken token)
            {
                IDisposable operationRegistration = null;
                try
                {
                    operationRegistration = _operationTrackerService.Register(endpointInfo);
                    return await _dumpCompletionSource.Task;
                }
                finally
                {
                    operationRegistration?.Dispose();
                }
            }

            public void CompleteOperation()
            {
                _dumpCompletionSource.SetResult(new MemoryStream());
            }
        }
    }
}
