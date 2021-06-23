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

        private const string FileProviderName = "files";
        private const string FileProviderType = "fileSystem";
        private static readonly string EgressPath = Path.Combine(Path.GetTempPath(), "Egress");

        public EgressTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

        private static RootOptions GenerateEgress()
        {
            var egressProvider = new EgressProvider()
            {
                EgressType = FileProviderType
            };

            egressProvider.Properties.Add("DirectoryPath", EgressPath);

            var options = new RootOptions()
            {
                Egress = new EgressOptions
                {
                    Providers = new Dictionary<string, EgressProvider>
                    {
                        { FileProviderName, egressProvider}
                    }
                }
            };

            return options;
        }

        [Fact]
        public async Task EgressTraceTest()
        {
            await ExecuteEgressScenario(async (ApiClient apiClient, AppRunner appRunner) =>
            {
                OperationResponse response = await apiClient.EgressTraceAsync(appRunner.ProcessId, durationSeconds: 5, FileProviderName).ConfigureAwait(false);
                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

                OperationStatus operationResult = await apiClient.PollOperationToCompletion(response.OperationUri);
                Assert.Equal(OperationState.Succeeded, operationResult.Status); 
            });
        }

        [Fact]
        public async Task EgressCancelTest()
        {
            await ExecuteEgressScenario(async (ApiClient apiClient, AppRunner appRunner) =>
            {
                OperationResponse response = await apiClient.EgressTraceAsync(appRunner.ProcessId, durationSeconds: -1, FileProviderName).ConfigureAwait(false);
                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

                OperationStatus operationResult = await apiClient.GetOperationStatus(response.OperationUri).ConfigureAwait(false);
                Assert.True(operationResult.Status == OperationState.Running);

                HttpStatusCode deleteStatus = await apiClient.CancelEgressOperation(response.OperationUri).ConfigureAwait(false);
                Assert.Equal(HttpStatusCode.OK, deleteStatus);
                operationResult = await apiClient.GetOperationStatus(response.OperationUri).ConfigureAwait(false);
                Assert.Equal(OperationState.Cancelled, operationResult.Status);
            });
        }

        private async Task ExecuteEgressScenario(Func<ApiClient, AppRunner, Task> func)
        {
            await using MonitorRunner toolRunner = new(_outputHelper);
            toolRunner.UseTempApiKey = true;
            toolRunner.WriteKeyPerValueConfiguration(GenerateEgress());

            await toolRunner.StartAsync();

            using HttpClient client = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
            ApiClient apiClient = new ApiClient(_outputHelper, client);

            await using AppRunner appRunner = new(_outputHelper, Assembly.GetExecutingAssembly());
            appRunner.ScenarioName = TestAppScenarios.AsyncWait.Name;
            await appRunner.ExecuteAsync(async () =>
            {
                await func(apiClient, appRunner);
                await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue).ConfigureAwait(false);
            });
        }

        public void Dispose()
        {
            try
            {
                if (!Directory.Exists(EgressPath))
                {
                    return;
                }
                foreach(string file in Directory.GetFiles(EgressPath))
                {
                    File.Delete(file);
                }
                Directory.Delete(EgressPath);
            }
            catch
            {
            }
        }
    }
}