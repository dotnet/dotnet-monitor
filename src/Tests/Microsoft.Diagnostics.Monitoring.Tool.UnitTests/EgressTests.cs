// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                    OperationResponse response = await apiClient.EgressTraceAsync(appRunner.ProcessId, durationSeconds: 5, FileProviderName).ConfigureAwait(false);
                    Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

                    OperationStatus operationResult = await apiClient.PollOperationToCompletion(response.OperationUri);
                    Assert.Equal(OperationState.Succeeded, operationResult.Status);
                    Assert.True(File.Exists(operationResult.ResourceLocation));

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
                    OperationResponse response = await apiClient.EgressTraceAsync(appRunner.ProcessId, durationSeconds: -1, FileProviderName).ConfigureAwait(false);
                    Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

                    OperationStatus operationResult = await apiClient.GetOperationStatus(response.OperationUri).ConfigureAwait(false);
                    Assert.True(operationResult.Status == OperationState.Running);

                    HttpStatusCode deleteStatus = await apiClient.CancelEgressOperation(response.OperationUri).ConfigureAwait(false);
                    Assert.Equal(HttpStatusCode.OK, deleteStatus);
                    operationResult = await apiClient.GetOperationStatus(response.OperationUri).ConfigureAwait(false);
                    Assert.Equal(OperationState.Cancelled, operationResult.Status);

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: (toolRunner) =>
                {
                    toolRunner.WriteKeyPerValueConfiguration(new RootOptions().AddFileSystemEgress(FileProviderName, _tempEgressPath.FullName));
                });
        }

        [Fact]
        public async Task ConcurrencyLimitTest()
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Connect,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (appRunner, apiClient) =>
                {
                    OperationResponse response1 = await EgressTraceWithDelay(apiClient, appRunner.ProcessId, HttpStatusCode.Accepted);
                    OperationResponse response2 = await EgressTraceWithDelay(apiClient, appRunner.ProcessId, HttpStatusCode.Accepted);
                    OperationResponse response3 = await EgressTraceWithDelay(apiClient, appRunner.ProcessId, HttpStatusCode.Accepted);
                    OperationResponse response = await EgressTraceWithDelay(apiClient, appRunner.ProcessId, HttpStatusCode.TooManyRequests);

                    await CancelEgressOperation(apiClient, response1);
                    await CancelEgressOperation(apiClient, response2);

                    OperationResponse response4 = await EgressTraceWithDelay(apiClient, appRunner.ProcessId, HttpStatusCode.Accepted, delay: true);

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
                    OperationResponse response1 = await EgressTraceWithDelay(apiClient, appRunner.ProcessId, HttpStatusCode.Accepted);
                    OperationResponse response3 = await EgressTraceWithDelay(apiClient, appRunner.ProcessId, HttpStatusCode.Accepted);
                    using HttpResponseMessage traceDirect1 = await TraceWithDelay(apiClient, appRunner.ProcessId);
                    Assert.Equal(HttpStatusCode.OK, traceDirect1.StatusCode);

                    OperationResponse response = await EgressTraceWithDelay(apiClient, appRunner.ProcessId, HttpStatusCode.TooManyRequests);
                    using HttpResponseMessage traceDirect = await TraceWithDelay(apiClient, appRunner.ProcessId);
                    Assert.Equal(HttpStatusCode.TooManyRequests, traceDirect.StatusCode);

                    //Validate that the failure from a direct call (handled by middleware)
                    //matches the failure produces by egress operations (handled by the Mvc ActionResult stack)
                    Assert.Equal(response.ResponseBody, await traceDirect.Content.ReadAsStringAsync());

                    await CancelEgressOperation(apiClient, response1);
                    OperationResponse response4 = await EgressTraceWithDelay(apiClient, appRunner.ProcessId, HttpStatusCode.Accepted, delay: true);

                    await CancelEgressOperation(apiClient, response3);
                    await CancelEgressOperation(apiClient, response4);

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: (toolRunner) =>
                {
                    toolRunner.WriteKeyPerValueConfiguration(new RootOptions().AddFileSystemEgress(FileProviderName, _tempEgressPath.FullName));
                });
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

        private async Task<OperationResponse> EgressTraceWithDelay(ApiClient apiClient, int processId, HttpStatusCode expectedCode, bool delay = true)
        {
            OperationResponse response = await apiClient.EgressTraceAsync(processId, durationSeconds: -1, FileProviderName).ConfigureAwait(false);
            Assert.Equal(expectedCode, response.StatusCode);

            if (delay)
            {
                //Wait 1 second to make sure the file names do not collide
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            return response;
        }

        private async Task CancelEgressOperation(ApiClient apiClient, OperationResponse response)
        {
            HttpStatusCode deleteStatus = await apiClient.CancelEgressOperation(response.OperationUri).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.OK, deleteStatus);
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