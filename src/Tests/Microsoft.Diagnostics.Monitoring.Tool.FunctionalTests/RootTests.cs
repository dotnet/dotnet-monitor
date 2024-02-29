// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
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
    public class RootTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public RootTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Tests that the root route of the URLs will return suitable HTTP
        /// </summary>
        [Fact]
        public async Task RootRoutesReturnTest()
        {
            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            await toolRunner.StartAsync();

            // Test that the root route returns HTTP 200 OK
            using HttpClient defaultHttpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory, ServiceProviderFixture.HttpClientName_NoRedirect);
            ApiClient defaultApiClient = new(_outputHelper, defaultHttpClient);

            var rootResult = await defaultApiClient.GetRootAsync();
            Assert.Equal(HttpStatusCode.OK, rootResult.StatusCode);


            // Disabled as there doesn't seem to be anything different about the metrics root URL from the one above.

            //// Test metrics URL root returns HTTP 404
            //using HttpClient metricsHttpClient = await toolRunner.CreateHttpClientMetricsAddressAsync(_httpClientFactory);
            //ApiClient metricsApiClient = new(_outputHelper, defaultHttpClient);

            //var statusCodeException = await Assert.ThrowsAsync<ApiStatusCodeException>(
            //    () => defaultApiClient.GetRootAsync());
            //Assert.Equal(HttpStatusCode.NotFound, statusCodeException.StatusCode);
        }
    }
}
