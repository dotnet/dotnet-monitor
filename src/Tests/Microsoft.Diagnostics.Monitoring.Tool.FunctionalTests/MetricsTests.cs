// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Graphs;
using Microsoft.AspNetCore.Http;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class MetricsTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public MetricsTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Tests that turning off metrics via the command line will have the /metrics route not serve metrics.
        /// </summary>
        [Fact]
        public async Task DisableMetricsViaCommandLineTest()
        {
            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.DisableMetricsViaCommandLine = true;
            await toolRunner.StartAsync();

            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
            ApiClient apiClient = new(_outputHelper, httpClient);

            // Check that /metrics does not serve metrics
            var validationProblemDetailsException = await Assert.ThrowsAsync<ValidationProblemDetailsException>(
                () => apiClient.GetMetricsAsync());
            Assert.Equal(HttpStatusCode.BadRequest, validationProblemDetailsException.StatusCode);
            Assert.Equal(StatusCodes.Status400BadRequest, validationProblemDetailsException.Details.Status);
        }

        /// <summary>
        /// Tests that turning off metrics via configuration will have the /metrics route not serve metrics.
        /// </summary>
        [Fact]
        public async Task DisableMetricsViaEnvironmentTest()
        {
            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.ConfigurationFromEnvironment.Metrics = new()
            {
                Enabled = false
            };
            await toolRunner.StartAsync();

            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
            ApiClient apiClient = new(_outputHelper, httpClient);

            // Check that /metrics does not serve metrics
            var validationProblemDetailsException = await Assert.ThrowsAsync<ValidationProblemDetailsException>(
                () => apiClient.GetMetricsAsync());
            Assert.Equal(HttpStatusCode.BadRequest, validationProblemDetailsException.StatusCode);
            Assert.Equal(StatusCodes.Status400BadRequest, validationProblemDetailsException.Details.Status);
        }

        /// <summary>
        /// Tests that turning off metrics via settings will have the /metrics route not serve metrics.
        /// </summary>
        [Fact]
        public async Task DisableMetricsViaSettingsTest()
        {
            await using MonitorCollectRunner toolRunner = new(_outputHelper);

            await toolRunner.WriteUserSettingsAsync(new RootOptions()
            {
                Metrics = new MetricsOptions()
                {
                    Enabled = false
                }
            });

            await toolRunner.StartAsync();

            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
            ApiClient client = new(_outputHelper, httpClient);

            // Check that /metrics does not serve metrics
            var validationProblemDetailsException = await Assert.ThrowsAsync<ValidationProblemDetailsException>(
                () => client.GetMetricsAsync());
            Assert.Equal(HttpStatusCode.BadRequest, validationProblemDetailsException.StatusCode);
            Assert.Equal(StatusCodes.Status400BadRequest, validationProblemDetailsException.Details.Status);
        }

        /// <summary>
        /// Tests that turning off metrics via key-per-file will have the /metrics route not serve metrics.
        /// </summary>
        [Fact]
        public async Task DisableMetricsViaKeyPerFileTest()
        {
            await using MonitorCollectRunner toolRunner = new(_outputHelper);

            toolRunner.WriteKeyPerValueConfiguration(new RootOptions()
            {
                Metrics = new MetricsOptions()
                {
                    Enabled = false
                }
            });

            await toolRunner.StartAsync();

            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
            ApiClient apiClient = new(_outputHelper, httpClient);

            // Check that /metrics does not serve metrics
            var validationProblemDetailsException = await Assert.ThrowsAsync<ValidationProblemDetailsException>(
                () => apiClient.GetMetricsAsync());
            Assert.Equal(HttpStatusCode.BadRequest, validationProblemDetailsException.StatusCode);
            Assert.Equal(StatusCodes.Status400BadRequest, validationProblemDetailsException.Details.Status);
        }

        [Fact]
        public async Task SystemDiagnosticsMetricsTest()
        {
            Task startCollectLogsTask = null;
            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                mode,
                TestAppScenarios.Metrics.Name,
                appValidate: async (runner, client) =>
                {
                    Task<ResponseStreamHolder> holderTask = client.CaptureMetricsAsync(await runner.ProcessIdTask, 5);

                    await startCollectLogsTask;

                    // Start logging in the target application
                    await runner.SendCommandAsync(TestAppScenarios.Logger.Commands.StartLogging);

                    // Await the holder after sending the message to start logging so that ASP.NET can send chunked responses.
                    // If awaited before sending the message, ASP.NET will not send the complete set of headers because no data
                    // is written into the response stream. Since HttpClient.SendAsync has to wait for the complete set of headers,
                    // the /logs invocation would run and complete with no log events. To avoid this, the /logs invocation is started,
                    // then the StartLogging message is sent, and finally the holder is awaited.
                    using ResponseStreamHolder holder = await holderTask;
                    Assert.NotNull(holder);

                    await LogsTestUtilities.ValidateLogsEquality(holder.Stream, callback, logFormat, _outputHelper);

                    // Note: Do not wait for completion of the HTTP response. No more relevant data will be produced;
                    // the code would only be waiting for the response to end. Ideally the operation is gracefully stopped at
                    // this point.
                },
                configureTool: runner =>
                {
                    startCollectLogsTask = runner.WaitForStartCollectLogsAsync();
                });
        }
    }
}
