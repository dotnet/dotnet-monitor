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
                    OperationResponse response1 = await apiClient.EgressTraceAsync(appRunner.ProcessId, durationSeconds: -1, FileProviderName).ConfigureAwait(false);
                    Assert.Equal(HttpStatusCode.Accepted, response1.StatusCode);

                    //Wait 1 second to make sure the file names do not collide
                    await Task.Delay(1000);

                    OperationResponse response2 = await apiClient.EgressTraceAsync(appRunner.ProcessId, durationSeconds: -1, FileProviderName).ConfigureAwait(false);
                    Assert.Equal(HttpStatusCode.Accepted, response2.StatusCode);

                    await Task.Delay(1000);

                    OperationResponse response3 = await apiClient.EgressTraceAsync(appRunner.ProcessId, durationSeconds: -1, FileProviderName).ConfigureAwait(false);
                    Assert.Equal(HttpStatusCode.Accepted, response3.StatusCode);

                    await Task.Delay(1000);

                    OperationResponse response = await apiClient.EgressTraceAsync(appRunner.ProcessId, durationSeconds: -1, FileProviderName).ConfigureAwait(false);
                    Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);

                    HttpStatusCode deleteStatus1 = await apiClient.CancelEgressOperation(response1.OperationUri).ConfigureAwait(false);
                    Assert.Equal(HttpStatusCode.OK, deleteStatus1);

                    HttpStatusCode deleteStatus2 = await apiClient.CancelEgressOperation(response2.OperationUri).ConfigureAwait(false);
                    Assert.Equal(HttpStatusCode.OK, deleteStatus2);

                    await Task.Delay(1000);

                    OperationResponse response4 = await apiClient.EgressTraceAsync(appRunner.ProcessId, durationSeconds: -1, FileProviderName).ConfigureAwait(false);
                    Assert.Equal(HttpStatusCode.Accepted, response4.StatusCode);

                    HttpStatusCode deleteStatus3 = await apiClient.CancelEgressOperation(response3.OperationUri).ConfigureAwait(false);
                    Assert.Equal(HttpStatusCode.OK, deleteStatus3);

                    HttpStatusCode deleteStatus4 = await apiClient.CancelEgressOperation(response4.OperationUri).ConfigureAwait(false);
                    Assert.Equal(HttpStatusCode.OK, deleteStatus4);

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: (toolRunner) =>
                {
                    toolRunner.WriteKeyPerValueConfiguration(new RootOptions().AddFileSystemEgress(FileProviderName, _tempEgressPath.FullName));
                });
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