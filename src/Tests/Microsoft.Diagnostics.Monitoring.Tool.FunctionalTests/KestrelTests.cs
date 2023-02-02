// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public sealed class KestrelTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public KestrelTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

        [Fact]
        public Task UrlBinding_NoOverrideWarning_CmdLine()
        {
            return ExecuteNoOverrideWarning();
        }

        [Fact]
        public Task UrlBinding_NoOverrideWarning_DotNet()
        {
            return ExecuteNoOverrideWarning((runner, url) =>
            {
                runner.DotNetUrls = url;
            });
        }

        [Fact]
        public Task UrlBinding_NoOverrideWarning_AspNetCore()
        {
            return ExecuteNoOverrideWarning((runner, url) =>
            {
                // This should be overridden by the ASPNETCORE_Urls entry. If it is not,
                // it will cause dotnet-monitor to not bind correctly and the /info route
                // check will fail.
                runner.DotNetUrls = "dotnet_invalid";
                runner.AspNetCoreUrls = url;
            });
        }

        [Fact]
        public Task UrlBinding_NoOverrideWarning_DotNetMonitor()
        {
            return ExecuteNoOverrideWarning((runner, url) =>
            {
                // These should be overridden by the DotnetMonitor_Urls entry. If it is not,
                // it will cause dotnet-monitor to not bind correctly and the /info route
                // check will fail.
                runner.DotNetUrls = "dotnet_invalid";
                runner.AspNetCoreUrls = "aspnetcore_invalid";

                runner.DotNetMonitorUrls = url;
            });
        }

        private async Task ExecuteNoOverrideWarning(Action<MonitorCollectRunner, string> configure = null)
        {
            await using MonitorCollectRunner runner = new(_outputHelper);

            runner.DisableAuthentication = true;

            configure?.Invoke(runner, "http://+:0");

            await runner.StartAsync();

            string address = await runner.GetDefaultAddressAsync(CancellationToken.None);
            UriBuilder builder = new(address);
            builder.Host = "localhost";

            using HttpClient httpClient = await runner.CreateHttpClientAsync(_httpClientFactory, builder.Uri.ToString());
            ApiClient apiClient = new(_outputHelper, httpClient);

            // Test that the route (thus the URL) is viable
            DotnetMonitorInfo info = await apiClient.GetInfoAsync();
            Assert.NotNull(info);

            // Test that the URL override warning is not present
            Assert.False(runner.OverrodeServerUrls, "Override URL warning should not be present.");
        }
    }
}
