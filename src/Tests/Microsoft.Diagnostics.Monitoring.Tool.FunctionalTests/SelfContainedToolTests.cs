// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    /// <summary>
    /// Functional tests that run dotnet-monitor as its self-contained, single-file, trimmed build instead of
    /// the framework-dependent build. These tests prove that the self-contained host can be discovered and
    /// launched directly, that the functional-test injection chain (test startup hook plus ASP.NET Core
    /// hosting startup) still works under a trimmed single-file host, and that representative diagnostic
    /// collection paths function end-to-end.
    /// </summary>
    /// <remarks>
    /// These tests are skipped unless a self-contained build is available (see <see cref="SelfContainedToolHelper"/>);
    /// publish one to the convention path or set the <c>DotNetMonitorTestSelfContainedToolPath</c> environment
    /// variable to enable them. Setting that environment variable also flips the entire functional suite to run
    /// against the self-contained host, so these dedicated tests provide focused positive coverage even when the
    /// suite-wide flip is not in effect.
    /// </remarks>
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class SelfContainedToolTests : IDisposable
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;
        private readonly TemporaryDirectory _tempDirectory;

        private const string FileProviderName = "files";

        public SelfContainedToolTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
            _tempDirectory = new(outputHelper);
        }

        /// <summary>
        /// Verifies that the self-contained host starts, the hosting-startup DI overrides participate, and the
        /// info endpoint responds. This is the broadest smoke test of the injection chain under a trimmed,
        /// single-file, self-contained host.
        /// </summary>
        [ConditionalTheory(typeof(TestConditions), nameof(TestConditions.IsSelfContainedToolAvailable))]
        [InlineData(DiagnosticPortConnectionMode.Connect)]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public Task SelfContainedInfoTest(DiagnosticPortConnectionMode mode)
        {
            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                mode,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (runner, client) =>
                {
                    DotnetMonitorInfo info = await client.GetInfoAsync();

                    Assert.NotNull(info.Version);
                    Assert.True(Version.TryParse(info.RuntimeVersion, out _), "Unable to parse version from RuntimeVersion property.");
                    Assert.Equal(mode, info.DiagnosticPortMode);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: ForceSelfContained);
        }

        /// <summary>
        /// Verifies that the self-contained host can discover a target process over the diagnostic pipe and
        /// report it through the processes endpoint.
        /// </summary>
        [ConditionalTheory(typeof(TestConditions), nameof(TestConditions.IsSelfContainedToolAvailable))]
        [InlineData(DiagnosticPortConnectionMode.Connect)]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public Task SelfContainedProcessesTest(DiagnosticPortConnectionMode mode)
        {
            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                mode,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (runner, client) =>
                {
                    int processId = await runner.ProcessIdTask;

                    IEnumerable<ProcessIdentifier> processes = await client.GetProcessesAsync();
                    Assert.NotNull(processes);
                    Assert.Contains(processes, p => p.Pid == processId);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: ForceSelfContained);
        }

        /// <summary>
        /// Verifies that the self-contained host can collect a trace via EventPipe and egress it to the file
        /// system, exercising the collection pipeline and the reflection-based operation-status serialization
        /// under full trimming.
        /// </summary>
        [ConditionalFact(typeof(TestConditions), nameof(TestConditions.IsSelfContainedToolAvailable))]
        public Task SelfContainedEgressTraceTest()
        {
            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Connect,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (appRunner, apiClient) =>
                {
                    int processId = await appRunner.ProcessIdTask;

                    OperationResponse response = await apiClient.EgressTraceAsync(processId, durationSeconds: 5, FileProviderName);
                    Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

                    OperationStatusResponse operationResult = await apiClient.PollOperationToCompletion(response.OperationUri);
                    Assert.Equal(HttpStatusCode.Created, operationResult.StatusCode);
                    Assert.Equal(OperationState.Succeeded, operationResult.OperationStatus.Status);
                    Assert.True(File.Exists(operationResult.OperationStatus.ResourceLocation));

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: toolRunner =>
                {
                    ForceSelfContained(toolRunner);
                    toolRunner.WriteKeyPerValueConfiguration(new RootOptions().AddFileSystemEgress(FileProviderName, _tempDirectory.FullName));
                });
        }

        /// <summary>
        /// Verifies that the self-contained host can stream collected logs, exercising the in-process logging
        /// pipeline distinct from the trace path.
        /// </summary>
        [ConditionalTheory(typeof(TestConditions), nameof(TestConditions.IsSelfContainedToolAvailable))]
        [InlineData(LogFormat.NewlineDelimitedJson)]
        [InlineData(LogFormat.JsonSequence)]
        public Task SelfContainedLogsTest(LogFormat logFormat)
        {
            Task startCollectLogsTask = null;
            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                TestAppScenarios.Logger.Name,
                appValidate: async (runner, client) =>
                {
                    Task<ResponseStreamHolder> holderTask = client.CaptureLogsAsync(
                        await runner.ProcessIdTask,
                        CommonTestTimeouts.LogsDuration,
                        new LogsConfiguration() { LogLevel = LogLevel.Information, UseAppFilters = false },
                        logFormat);

                    await startCollectLogsTask;

                    await runner.SendCommandAsync(TestAppScenarios.Logger.Commands.StartLogging);

                    using ResponseStreamHolder holder = await holderTask;
                    Assert.NotNull(holder);

                    await LogsTestUtilities.ValidateLogsEquality(
                        holder.Stream,
                        async reader =>
                        {
                            // The self-contained host must produce at least one well-formed log entry.
                            Assert.True(await reader.WaitToReadAsync(), "Expected at least one log entry from the self-contained host.");
                            LogEntry entry = await reader.ReadAsync();
                            Assert.False(string.IsNullOrEmpty(entry.Category));
                        },
                        logFormat,
                        _outputHelper);
                },
                configureTool: toolRunner =>
                {
                    ForceSelfContained(toolRunner);
                    startCollectLogsTask = toolRunner.WaitForStartCollectLogsAsync();
                });
        }

        private static void ForceSelfContained(MonitorCollectRunner runner)
        {
            runner.SelfContained = true;
        }

        public void Dispose()
        {
            _tempDirectory.Dispose();
        }
    }
}
