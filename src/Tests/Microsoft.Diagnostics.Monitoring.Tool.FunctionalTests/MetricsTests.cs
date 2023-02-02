// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.DependencyInjection;
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
                apiClient.GetMetricsAsync);
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
                apiClient.GetMetricsAsync);
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
                client.GetMetricsAsync);
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
                apiClient.GetMetricsAsync);
            Assert.Equal(HttpStatusCode.BadRequest, validationProblemDetailsException.StatusCode);
            Assert.Equal(StatusCodes.Status400BadRequest, validationProblemDetailsException.Details.Status);
        }
    }
}
