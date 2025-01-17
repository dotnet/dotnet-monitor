// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class EgressTests : IDisposable
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;
        private readonly TemporaryDirectory _tempDirectory;

        private const string FileProviderName = "files";

        // This should be identical to the error message found in Strings.resx
        private const string DisabledHTTPEgressErrorMessage = "HTTP egress is not enabled.";

        public EgressTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
            _tempDirectory = new(outputHelper);
        }

        [Fact]
        public async Task EgressTraceTest()
        {
            await ScenarioRunner.SingleTarget(
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
                configureTool: (toolRunner) =>
                {
                    toolRunner.WriteKeyPerValueConfiguration(new RootOptions().AddFileSystemEgress(FileProviderName, _tempDirectory.FullName));
                });
        }

        [Fact]
        public async Task EgressCancelTest()
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Connect,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (appRunner, apiClient) =>
                {
                    int processId = await appRunner.ProcessIdTask;

                    OperationResponse response = await apiClient.EgressTraceAsync(processId, durationSeconds: -1, FileProviderName);
                    Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

                    OperationStatusResponse operationResult = await apiClient.WaitForOperationToStart(response.OperationUri);
                    Assert.Equal(HttpStatusCode.OK, operationResult.StatusCode);
                    Assert.True(operationResult.OperationStatus.Status == OperationState.Running);

                    HttpStatusCode deleteStatus = await apiClient.CancelEgressOperation(response.OperationUri);
                    Assert.Equal(HttpStatusCode.OK, deleteStatus);

                    operationResult = await apiClient.GetOperationStatus(response.OperationUri);
                    Assert.Equal(HttpStatusCode.OK, operationResult.StatusCode);
                    Assert.Equal(OperationState.Cancelled, operationResult.OperationStatus.Status);
                    Assert.False(operationResult.OperationStatus.IsStoppable);

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: (toolRunner) =>
                {
                    toolRunner.WriteKeyPerValueConfiguration(new RootOptions().AddFileSystemEgress(FileProviderName, _tempDirectory.FullName));
                });
        }

        [Fact]
        public async Task EgressStopTest()
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Connect,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (appRunner, apiClient) =>
                {
                    int processId = await appRunner.ProcessIdTask;

                    OperationResponse response = await apiClient.EgressTraceAsync(processId, durationSeconds: -1, FileProviderName);
                    Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

                    OperationStatusResponse operationResult = await apiClient.WaitForOperationToStart(response.OperationUri);
                    Assert.Equal(HttpStatusCode.OK, operationResult.StatusCode);
                    Assert.Equal(OperationState.Running, operationResult.OperationStatus.Status);
                    Assert.True(operationResult.OperationStatus.IsStoppable);

                    HttpStatusCode deleteStatus = await apiClient.StopEgressOperation(response.OperationUri);
                    Assert.Equal(HttpStatusCode.Accepted, deleteStatus);

                    OperationStatusResponse pollOperationResult = await apiClient.PollOperationToCompletion(response.OperationUri);
                    Assert.Equal(HttpStatusCode.Created, pollOperationResult.StatusCode);
                    Assert.Equal(OperationState.Succeeded, pollOperationResult.OperationStatus.Status);
                    Assert.False(pollOperationResult.OperationStatus.IsStoppable);

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: (toolRunner) =>
                {
                    toolRunner.WriteKeyPerValueConfiguration(new RootOptions().AddFileSystemEgress(FileProviderName, _tempDirectory.FullName));
                });
        }

        // https://github.com/dotnet/dotnet-monitor/issues/1285
        [Fact]
        public async Task EgressListTest()
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Connect,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (appRunner, apiClient) =>
                {
                    int processId = await appRunner.ProcessIdTask;

                    OperationResponse response1 = await EgressTraceWithDelay(apiClient, processId);
                    OperationResponse response2 = await EgressTraceWithDelay(apiClient, processId, delay: false);
                    await CancelEgressOperation(apiClient, response2);

                    List<OperationSummary> result = await apiClient.GetOperations();
                    Assert.Equal(2, result.Count);

                    OperationStatusResponse status1 = await apiClient.GetOperationStatus(response1.OperationUri);
                    OperationSummary summary1 = result.First(os => os.OperationId == status1.OperationStatus.OperationId);
                    ValidateOperation(status1.OperationStatus, summary1);
                    Assert.True(summary1.IsStoppable);
                    Assert.Equal(FileProviderName, summary1.EgressProviderName);

                    OperationStatusResponse status2 = await apiClient.GetOperationStatus(response2.OperationUri);
                    OperationSummary summary2 = result.First(os => os.OperationId == status2.OperationStatus.OperationId);
                    ValidateOperation(status2.OperationStatus, summary2);
                    Assert.False(summary2.IsStoppable);
                    Assert.Equal(FileProviderName, summary2.EgressProviderName);

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: (toolRunner) =>
                {
                    toolRunner.WriteKeyPerValueConfiguration(new RootOptions().AddFileSystemEgress(FileProviderName, _tempDirectory.FullName));
                });
        }

        [Fact(Skip = "https://github.com/dotnet/dotnet-monitor/issues/586")]
        public async Task ConcurrencyLimitTest()
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Connect,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (appRunner, apiClient) =>
                {
                    int processId = await appRunner.ProcessIdTask;

                    OperationResponse response1 = await EgressTraceWithDelay(apiClient, processId);
                    OperationResponse response2 = await EgressTraceWithDelay(apiClient, processId);
                    OperationResponse response3 = await EgressTraceWithDelay(apiClient, processId);

                    ValidationProblemDetailsException ex = await Assert.ThrowsAsync<ValidationProblemDetailsException>(() => EgressTraceWithDelay(apiClient, processId));
                    Assert.Equal(HttpStatusCode.TooManyRequests, ex.StatusCode);
                    Assert.Equal((int)HttpStatusCode.TooManyRequests, ex.Details.Status.GetValueOrDefault());

                    await CancelEgressOperation(apiClient, response1);
                    await CancelEgressOperation(apiClient, response2);

                    OperationResponse response4 = await EgressTraceWithDelay(apiClient, processId, delay: false);

                    await CancelEgressOperation(apiClient, response3);
                    await CancelEgressOperation(apiClient, response4);

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: (toolRunner) =>
                {
                    toolRunner.WriteKeyPerValueConfiguration(new RootOptions().AddFileSystemEgress(FileProviderName, _tempDirectory.FullName));
                });
        }

        [Fact]
        public async Task SharedConcurrencyLimitTest()
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Connect,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (appRunner, apiClient) =>
                {
                    int processId = await appRunner.ProcessIdTask;

                    OperationResponse response1 = await EgressTraceWithDelay(apiClient, processId);
                    OperationResponse response2 = await EgressTraceWithDelay(apiClient, processId);
                    using HttpResponseMessage traceDirect1 = await TraceWithDelay(apiClient, processId);
                    Assert.Equal(HttpStatusCode.OK, traceDirect1.StatusCode);

                    ValidationProblemDetailsException ex = await Assert.ThrowsAsync<ValidationProblemDetailsException>(
                        () => EgressTraceWithDelay(apiClient, processId, delay: false));
                    Assert.Equal(HttpStatusCode.TooManyRequests, ex.StatusCode);

                    using HttpResponseMessage traceDirect = await TraceWithDelay(apiClient, processId, delay: false);
                    Assert.Equal(HttpStatusCode.TooManyRequests, traceDirect.StatusCode);

                    //Validate that the failure from a direct call (handled by middleware)
                    //matches the failure produces by egress operations (handled by the Mvc ActionResult stack)
                    using HttpResponseMessage egressDirect = await EgressDirect(apiClient, processId);
                    Assert.Equal(HttpStatusCode.TooManyRequests, egressDirect.StatusCode);
                    Assert.Equal(await egressDirect.Content.ReadAsStringAsync(), await traceDirect.Content.ReadAsStringAsync());

                    await CancelEgressOperation(apiClient, response1);
                    OperationResponse response3 = await EgressTraceWithDelay(apiClient, processId, delay: false);

                    await CancelEgressOperation(apiClient, response2);
                    await CancelEgressOperation(apiClient, response3);

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: (toolRunner) =>
                {
                    toolRunner.WriteKeyPerValueConfiguration(new RootOptions().AddFileSystemEgress(FileProviderName, _tempDirectory.FullName));
                });
        }

        [Fact]
        public async Task HttpEgressCancelTest()
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Connect,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (appRunner, apiClient) =>
                {
                    int processId = await appRunner.ProcessIdTask;

                    using ResponseStreamHolder responseBox = await apiClient.HttpEgressTraceAsync(processId, durationSeconds: -1);

                    Uri operationUri = responseBox.Response.Headers.Location;
                    Assert.NotNull(operationUri);

                    // Start consuming the stream
                    Task drainResponseTask = responseBox.Stream.CopyToAsync(Stream.Null);

                    // Make sure the operation exists
                    OperationStatusResponse operationResult = await apiClient.WaitForOperationToStart(operationUri);
                    Assert.Equal(HttpStatusCode.OK, operationResult.StatusCode);
                    Assert.True(operationResult.OperationStatus.Status == OperationState.Running);

                    // Cancel the trace operation
                    HttpStatusCode deleteStatus = await apiClient.CancelEgressOperation(operationUri);
                    Assert.Equal(HttpStatusCode.OK, deleteStatus);

                    operationResult = await apiClient.GetOperationStatus(operationUri);
                    Assert.Equal(HttpStatusCode.OK, operationResult.StatusCode);
                    Assert.Equal(OperationState.Cancelled, operationResult.OperationStatus.Status);

                    // In .NET 8+ this will throw an HttpIOException, which is derived from IOException
                    await Assert.ThrowsAnyAsync<IOException>(() => drainResponseTask);

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: (toolRunner) =>
                {
                    toolRunner.WriteKeyPerValueConfiguration(new RootOptions().AddFileSystemEgress(FileProviderName, _tempDirectory.FullName));
                });
        }

        [Fact]
        public async Task HttpEgressStopTest()
        {
            using TemporaryDirectory tempDir = new(_outputHelper);

            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Connect,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (appRunner, apiClient) =>
                {
                    int processId = await appRunner.ProcessIdTask;

                    using ResponseStreamHolder responseBox = await apiClient.HttpEgressTraceAsync(processId, durationSeconds: -1);

                    Uri operationUri = responseBox.Response.Headers.Location;
                    Assert.NotNull(operationUri);

                    // Start saving the stream
                    string traceFile = Path.Combine(tempDir.FullName, "test.nettrace");
                    using FileStream traceFileWriter = File.OpenWrite(traceFile);

                    Task drainResponseTask = responseBox.Stream.CopyToAsync(traceFileWriter);

                    // Make sure the operation exists
                    OperationStatusResponse operationResult = await apiClient.WaitForOperationToStart(operationUri);
                    Assert.Equal(HttpStatusCode.OK, operationResult.StatusCode);
                    Assert.True(operationResult.OperationStatus.Status == OperationState.Running);

                    // Stop the trace operation
                    HttpStatusCode deleteStatus = await apiClient.StopEgressOperation(operationUri);
                    Assert.Equal(HttpStatusCode.Accepted, deleteStatus);

                    using CancellationTokenSource timeoutCancellation = new(CommonTestTimeouts.TraceTimeout);
                    await drainResponseTask.WaitAsync(timeoutCancellation.Token);
                    await traceFileWriter.DisposeAsync();

                    operationResult = await apiClient.PollOperationToCompletion(operationUri);
                    Assert.Equal(HttpStatusCode.Created, operationResult.StatusCode);
                    Assert.Equal(OperationState.Succeeded, operationResult.OperationStatus.Status);

                    // Verify the resulting trace has rundown information
                    using FileStream traceStream = File.OpenRead(traceFile);
                    await TraceTestUtilities.ValidateTrace(traceStream, expectRundown: true);

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: (toolRunner) =>
                {
                    toolRunner.WriteKeyPerValueConfiguration(new RootOptions().AddFileSystemEgress(FileProviderName, _tempDirectory.FullName));
                });
        }

        /// <summary>
        /// Tests that turning off HTTP egress results in an error for dumps and logs (gcdumps and traces are currently not tested)
        /// </summary>
        [Fact]
        public async Task DisableHttpEgressTest()
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Connect,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (appRunner, appClient) =>
                {
                    int processId = await appRunner.ProcessIdTask;

                    ProcessInfo processInfo = await appClient.GetProcessAsync(processId);
                    Assert.NotNull(processInfo);

                    // Dump Error Check
                    ValidationProblemDetailsException validationProblemDetailsExceptionDumps = await Assert.ThrowsAsync<ValidationProblemDetailsException>(
                        () => appClient.CaptureDumpAsync(processId, DumpType.Mini));
                    Assert.Equal(HttpStatusCode.BadRequest, validationProblemDetailsExceptionDumps.StatusCode);
                    Assert.Equal(StatusCodes.Status400BadRequest, validationProblemDetailsExceptionDumps.Details.Status);
                    Assert.Equal(DisabledHTTPEgressErrorMessage, validationProblemDetailsExceptionDumps.Message);

                    // Logs Error Check
                    ValidationProblemDetailsException validationProblemDetailsExceptionLogs = await Assert.ThrowsAsync<ValidationProblemDetailsException>(
                            () => appClient.CaptureLogsAsync(processId, CommonTestTimeouts.LogsDuration, LogLevel.None, LogFormat.NewlineDelimitedJson));
                    Assert.Equal(HttpStatusCode.BadRequest, validationProblemDetailsExceptionLogs.StatusCode);
                    Assert.Equal(StatusCodes.Status400BadRequest, validationProblemDetailsExceptionLogs.Details.Status);
                    Assert.Equal(DisabledHTTPEgressErrorMessage, validationProblemDetailsExceptionLogs.Message);

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                disableHttpEgress: true);
        }

        /// <summary>
        /// Test that when requesting non-existent egress it immediately returns HTTP 400
        /// rather than queueing the request and having the operation report that it failed.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task EgressNotExistTest()
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Connect,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (appRunner, apiClient) =>
                {
                    int processId = await appRunner.ProcessIdTask;

                    ValidationProblemDetailsException validationException = await Assert.ThrowsAsync<ValidationProblemDetailsException>(
                        () => apiClient.EgressTraceAsync(processId, durationSeconds: 5, FileProviderName));
                    Assert.Equal(HttpStatusCode.BadRequest, validationException.StatusCode);

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                });
        }

        private async Task<HttpResponseMessage> TraceWithDelay(ApiClient client, int processId, bool delay = true)
        {
            HttpResponseMessage message = await RetryUtilities.RetryAsync(
                func: () => client.ApiCall(FormattableString.Invariant($"/trace?pid={processId}&durationSeconds=-1")),
                shouldRetry: IsTransientApiFailure,
                outputHelper: _outputHelper);

            if (delay)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            return message;
        }

        private async Task<HttpResponseMessage> EgressDirect(ApiClient client, int processId)
        {
            return await RetryUtilities.RetryAsync(
                func: () => client.ApiCall(FormattableString.Invariant($"/trace?pid={processId}&egressProvider={FileProviderName}")),
                shouldRetry: IsTransientApiFailure,
                outputHelper: _outputHelper);
        }

        private async Task<OperationResponse> EgressTraceWithDelay(ApiClient apiClient, int processId, bool delay = true)
        {
            try
            {
                return await RetryUtilities.RetryAsync(
                    func: () => apiClient.EgressTraceAsync(processId, durationSeconds: -1, FileProviderName),
                    shouldRetry: IsTransientApiFailure,
                    outputHelper: _outputHelper);
            }
            finally
            {
                if (delay)
                {
                    //Wait 1 second to make sure the file names do not collide
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
        }

        private static async Task CancelEgressOperation(ApiClient apiClient, OperationResponse response)
        {
            HttpStatusCode deleteStatus = await apiClient.CancelEgressOperation(response.OperationUri);
            Assert.Equal(HttpStatusCode.OK, deleteStatus);
        }

        private static void ValidateOperation(OperationStatus expected, OperationSummary summary)
        {
            Assert.Equal(expected.OperationId, summary.OperationId);
            Assert.Equal(expected.Status, summary.Status);
            Assert.Equal(expected.CreatedDateTime, summary.CreatedDateTime);
            Assert.Equal(expected.EgressProviderName, summary.EgressProviderName);
            Assert.Equal(expected.IsStoppable, summary.IsStoppable);
        }

        // When the process could not be found (due to transient responsiveness issues), dotnet-monitor APIs will return a 400 status code.
        private static bool IsTransientApiFailure(Exception ex)
            => ex is ValidationProblemDetailsException validationException
            && validationException.StatusCode == HttpStatusCode.BadRequest;

        public void Dispose()
        {
            _tempDirectory.Dispose();
        }
    }
}
