// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.UnitTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.UnitTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.UnitTests.Models;
using Microsoft.Diagnostics.Monitoring.UnitTests.Options;
using Microsoft.Diagnostics.Monitoring.UnitTests.Runners;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FileFormats;
using Microsoft.FileFormats.ELF;
using Microsoft.FileFormats.MachO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.UnitTests
{
    [Collection(DefaultCollectionFixture.Name)]
    public class EgressTests : IDisposable
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;
        private readonly DirectoryInfo _tempEgressPath;

        private const string FileProviderName = "files";

        public EgressTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
            _tempEgressPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "Egress", Guid.NewGuid().ToString()));
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
                    OperationResponse response = await apiClient.EgressTraceAsync(appRunner.ProcessId, durationSeconds: 5, FileProviderName);
                    Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

                    OperationStatusResponse operationResult = await apiClient.PollOperationToCompletion(response.OperationUri);
                    Assert.Equal(HttpStatusCode.Created, operationResult.StatusCode);
                    Assert.Equal(OperationState.Succeeded, operationResult.OperationStatus.Status);
                    Assert.True(File.Exists(operationResult.OperationStatus.ResourceLocation));

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: (toolRunner) =>
                {
                    toolRunner.WriteKeyPerValueConfiguration(new RootOptions().AddFileSystemEgress(FileProviderName, _tempEgressPath.FullName));
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
                    OperationResponse response = await apiClient.EgressTraceAsync(appRunner.ProcessId, durationSeconds: -1, FileProviderName);
                    Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

                    OperationStatusResponse operationResult = await apiClient.GetOperationStatus(response.OperationUri);
                    Assert.Equal(HttpStatusCode.OK, operationResult.StatusCode);
                    Assert.True(operationResult.OperationStatus.Status == OperationState.Running);

                    HttpStatusCode deleteStatus = await apiClient.CancelEgressOperation(response.OperationUri);
                    Assert.Equal(HttpStatusCode.OK, deleteStatus);

                    operationResult = await apiClient.GetOperationStatus(response.OperationUri);
                    Assert.Equal(HttpStatusCode.OK, operationResult.StatusCode);
                    Assert.Equal(OperationState.Cancelled, operationResult.OperationStatus.Status);

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: (toolRunner) =>
                {
                    toolRunner.WriteKeyPerValueConfiguration(new RootOptions().AddFileSystemEgress(FileProviderName, _tempEgressPath.FullName));
                });
        }

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
                    OperationResponse response1 = await EgressTraceWithDelay(apiClient, appRunner.ProcessId);
                    OperationResponse response2 = await EgressTraceWithDelay(apiClient, appRunner.ProcessId, delay: false);
                    await CancelEgressOperation(apiClient, response2);

                    List<OperationSummary> result = await apiClient.GetOperations();
                    Assert.Equal(2, result.Count);

                    OperationStatusResponse status1 = await apiClient.GetOperationStatus(response1.OperationUri);
                    OperationSummary summary1 = result.First(os => os.OperationId == status1.OperationStatus.OperationId);
                    ValidateOperation(status1.OperationStatus, summary1);

                    OperationStatusResponse status2 = await apiClient.GetOperationStatus(response2.OperationUri);
                    OperationSummary summary2 = result.First(os => os.OperationId == status2.OperationStatus.OperationId);
                    ValidateOperation(status2.OperationStatus, summary2);

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: (toolRunner) =>
                {
                    toolRunner.WriteKeyPerValueConfiguration(new RootOptions().AddFileSystemEgress(FileProviderName, _tempEgressPath.FullName));
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
                    OperationResponse response1 = await EgressTraceWithDelay(apiClient, appRunner.ProcessId);
                    OperationResponse response2 = await EgressTraceWithDelay(apiClient, appRunner.ProcessId);
                    OperationResponse response3 = await EgressTraceWithDelay(apiClient, appRunner.ProcessId);

                    ValidationProblemDetailsException ex = await Assert.ThrowsAsync<ValidationProblemDetailsException>(() => EgressTraceWithDelay(apiClient, appRunner.ProcessId));
                    Assert.Equal(HttpStatusCode.TooManyRequests, ex.StatusCode);
                    Assert.Equal((int)HttpStatusCode.TooManyRequests, ex.Details.Status.GetValueOrDefault());


                    await CancelEgressOperation(apiClient, response1);
                    await CancelEgressOperation(apiClient, response2);

                    OperationResponse response4 = await EgressTraceWithDelay(apiClient, appRunner.ProcessId, delay: false);

                    await CancelEgressOperation(apiClient, response3);
                    await CancelEgressOperation(apiClient, response4);

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: (toolRunner) =>
                {
                    toolRunner.WriteKeyPerValueConfiguration(new RootOptions().AddFileSystemEgress(FileProviderName, _tempEgressPath.FullName));
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
                    OperationResponse response1 = await EgressTraceWithDelay(apiClient, appRunner.ProcessId);
                    OperationResponse response3 = await EgressTraceWithDelay(apiClient, appRunner.ProcessId);
                    using HttpResponseMessage traceDirect1 = await TraceWithDelay(apiClient, appRunner.ProcessId);
                    Assert.Equal(HttpStatusCode.OK, traceDirect1.StatusCode);

                    ValidationProblemDetailsException ex = await Assert.ThrowsAsync<ValidationProblemDetailsException>(() => EgressTraceWithDelay(apiClient, appRunner.ProcessId, delay: false));
                    Assert.Equal(HttpStatusCode.TooManyRequests, ex.StatusCode);

                    using HttpResponseMessage traceDirect = await TraceWithDelay(apiClient, appRunner.ProcessId, delay: false);
                    Assert.Equal(HttpStatusCode.TooManyRequests, traceDirect.StatusCode);

                    //Validate that the failure from a direct call (handled by middleware)
                    //matches the failure produces by egress operations (handled by the Mvc ActionResult stack)
                    using HttpResponseMessage egressDirect = await EgressDirect(apiClient, appRunner.ProcessId);
                    Assert.Equal(HttpStatusCode.TooManyRequests, egressDirect.StatusCode);
                    Assert.Equal(await egressDirect.Content.ReadAsStringAsync(), await traceDirect.Content.ReadAsStringAsync());

                    await CancelEgressOperation(apiClient, response1);
                    OperationResponse response4 = await EgressTraceWithDelay(apiClient, appRunner.ProcessId, delay: false);

                    await CancelEgressOperation(apiClient, response3);
                    await CancelEgressOperation(apiClient, response4);

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: (toolRunner) =>
                {
                    toolRunner.WriteKeyPerValueConfiguration(new RootOptions().AddFileSystemEgress(FileProviderName, _tempEgressPath.FullName));
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
                    ProcessInfo processInfo = await appClient.GetProcessAsync(appRunner.ProcessId);
                    Assert.NotNull(processInfo);

                    // Dump Error Check
                    ValidationProblemDetailsException validationProblemDetailsExceptionDumps = await Assert.ThrowsAsync<ValidationProblemDetailsException>(
                        () => appClient.CaptureDumpAsync(appRunner.ProcessId, DumpType.Mini));
                    Assert.Equal(HttpStatusCode.BadRequest, validationProblemDetailsExceptionDumps.StatusCode);
                    Assert.Equal(StatusCodes.Status400BadRequest, validationProblemDetailsExceptionDumps.Details.Status);

                    // Logs Error Check
                    ValidationProblemDetailsException validationProblemDetailsExceptionLogs = await Assert.ThrowsAsync<ValidationProblemDetailsException>(
                            () => appClient.CaptureLogsAsync(appRunner.ProcessId, TestTimeouts.LogsDuration, Extensions.Logging.LogLevel.None, WebApi.LogFormat.NDJson));
                    Assert.Equal(HttpStatusCode.BadRequest, validationProblemDetailsExceptionLogs.StatusCode);
                    Assert.Equal(StatusCodes.Status400BadRequest, validationProblemDetailsExceptionLogs.Details.Status);

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                disableHttpEgress: true);
        }

        private async Task<HttpResponseMessage> TraceWithDelay(ApiClient client, int processId, bool delay = true)
        {
            HttpResponseMessage message = await client.ApiCall(FormattableString.Invariant($"/trace?pid={processId}&durationSeconds=-1"));
            if (delay)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            return message;
        }

        private Task<HttpResponseMessage> EgressDirect(ApiClient client, int processId)
        {
            return client.ApiCall(FormattableString.Invariant($"/trace?pid={processId}&egressProvider={FileProviderName}"));
        }

        private async Task<OperationResponse> EgressTraceWithDelay(ApiClient apiClient, int processId, bool delay = true)
        {
            try
            {
                OperationResponse response = await apiClient.EgressTraceAsync(processId, durationSeconds: -1, FileProviderName);
                return response;
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

        private async Task CancelEgressOperation(ApiClient apiClient, OperationResponse response)
        {
            HttpStatusCode deleteStatus = await apiClient.CancelEgressOperation(response.OperationUri);
            Assert.Equal(HttpStatusCode.OK, deleteStatus);
        }

        private void ValidateOperation(OperationStatus expected, OperationSummary summary)
        {
            Assert.Equal(expected.OperationId, summary.OperationId);
            Assert.Equal(expected.Status, summary.Status);
            Assert.Equal(expected.CreatedDateTime, summary.CreatedDateTime);
        }

        public void Dispose()
        {
            try
            {
                _tempEgressPath?.Delete(recursive: true);
            }
            catch
            {
            }
        }
    }
}