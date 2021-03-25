﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.UnitTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.UnitTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.UnitTests.Options;
using Microsoft.Diagnostics.Monitoring.UnitTests.Runners;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.UnitTests
{
    [Collection(DefaultCollectionFixture.Name)]
    public class AuthenticationTests
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(1);

        private const string ApiKeyScheme = "MonitorApiKey";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public AuthenticationTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Tests the behavior of the default URL address, namely that all routes require
        /// authentication (401 Unauthorized) except for the metrics route (200 OK).
        /// </summary>
        [Fact]
        public async Task DefaultAddressTest()
        {
            await using DotNetMonitorRunner toolRunner = new(_outputHelper);
            
            await toolRunner.StartAsync(DefaultTimeout);

            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory, DefaultTimeout);
            ApiClient apiClient = new(_outputHelper, httpClient);

            // Any authenticated route on the default address should 401 Unauthenticated

            var statusCodeException = await Assert.ThrowsAsync<ApiStatusCodeException>(
                () => apiClient.GetProcessesAsync(DefaultTimeout));
            Assert.Equal(HttpStatusCode.Unauthorized, statusCodeException.StatusCode);

            // TODO: Verify other routes (e.g. /dump, /trace, /logs) also 401 Unauthenticated

            // Metrics should not throw on the default address

            var metrics = await apiClient.GetMetricsAsync(DefaultTimeout);
            Assert.NotNull(metrics);
        }

        /// <summary>
        /// Tests the behavior of the mertics URL address, namely that all routes will return
        /// 404 Not Found except for the metrics route (200 OK).
        /// </summary>
        [Fact]
        public async Task MetricsAddressTest()
        {
            await using DotNetMonitorRunner toolRunner = new(_outputHelper);

            await toolRunner.StartAsync(DefaultTimeout);

            using HttpClient httpClient = await toolRunner.CreateHttpClientMetricsAddressAsync(_httpClientFactory, DefaultTimeout);
            ApiClient apiClient = new(_outputHelper, httpClient);

            // Any non-metrics route on the metrics address should 404 Not Found
            var statusCodeException = await Assert.ThrowsAsync<ApiStatusCodeException>(
                () => apiClient.GetProcessesAsync(DefaultTimeout));
            Assert.Equal(HttpStatusCode.NotFound, statusCodeException.StatusCode);

            // TODO: Verify other routes (e.g. /dump, /trace, /logs) also 404 Not Found

            // Metrics should not throw on the metrics address
            var metrics = await apiClient.GetMetricsAsync(DefaultTimeout);
            Assert.NotNull(metrics);
        }

        /// <summary>
        /// Tests that turning off authentication allows access to all routes without authentication.
        /// </summary>
        [Fact]
        public async Task DisableAuthenticationTest()
        {
            await using DotNetMonitorRunner toolRunner = new(_outputHelper);
            toolRunner.DisableAuthentication = true;
            await toolRunner.StartAsync(DefaultTimeout);

            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory, DefaultTimeout);
            ApiClient apiClient = new(_outputHelper, httpClient);

            // Check that /processes does not challenge for authentication
            var processes = await apiClient.GetProcessesAsync(DefaultTimeout);
            Assert.NotNull(processes);

            // Check that /metrics does not challenge for authentication
            var metrics = await apiClient.GetMetricsAsync(DefaultTimeout);
            Assert.NotNull(metrics);
        }

        /// <summary>
        /// Tests that API key authentication can be configured correctly and
        /// that the key can be rotated wihtout shutting down dotnet-monitor.
        /// </summary>
        [Fact]
        public async Task ApiKeyAuthenticationSchemeTest()
        {
            const string AlgorithmName = "SHA256";

            await using DotNetMonitorRunner toolRunner = new(_outputHelper);

            _outputHelper.WriteLine("Generating API key.");

            // Generate initial API key
            byte[] apiKey = GenerateApiKey();

            // Set API key via key-per-file
            RootOptions options = new();
            options.UseApiKey(AlgorithmName, apiKey);
            toolRunner.WriteKeyPerValueConfiguration(options);

            // Start dotnet-monitor
            await toolRunner.StartAsync(DefaultTimeout);

            // Create HttpClient with default request headers
            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory, DefaultTimeout);
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(ApiKeyScheme, Convert.ToBase64String(apiKey));
            ApiClient apiClient = new(_outputHelper, httpClient);

            // Check that /processes does not challenge for authentication
            var processes = await apiClient.GetProcessesAsync(DefaultTimeout);
            Assert.NotNull(processes);

            _outputHelper.WriteLine("Rotating API key.");

            // Rotate the API key
            byte[] apiKey2 = GenerateApiKey();

            options.UseApiKey(AlgorithmName, apiKey2);
            toolRunner.WriteKeyPerValueConfiguration(options);

            // Wait for the key rotation to be consumed by dotnet-monitor; detect this
            // by checking for when API returns a 401. Ideally, key rotation would write
            // log event and runner monitor for event and notify.
            int attempts = 0;
            while (true)
            {
                attempts++;
                _outputHelper.WriteLine("Waiting for key rotation (attempt #{0}).", attempts);

                await Task.Delay(TimeSpan.FromSeconds(3));

                try
                {
                    await apiClient.GetProcessesAsync(DefaultTimeout);
                }
                catch (ApiStatusCodeException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
                {
                    break;
                }

                Assert.True(attempts < 10);
            }

            _outputHelper.WriteLine("Verifying new API key.");

            // Use new API key
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(ApiKeyScheme, Convert.ToBase64String(apiKey2));

            // Check that /processes does not challenge for authentication
            processes = await apiClient.GetProcessesAsync(DefaultTimeout);
            Assert.NotNull(processes);
        }

        /// <summary>
        /// Tests that Negotiate authentication can be used for authentication.
        /// </summary>
        [SkippableFact]
        public async Task NegotiateAuthenticationSchemeTest()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            await using DotNetMonitorRunner toolRunner = new(_outputHelper);
            await toolRunner.StartAsync(DefaultTimeout);

            // Create HttpClient and HttpClientHandler that uses the current
            // user's credentials from the test process. Since dotnet-monitor
            // is launched by the test process, the usage of these credentials
            // should authenticate correctly (except when elevated, which the
            // tool will deny authorization).
            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory, DefaultTimeout, ServiceProviderFixture.HttpClientName_DefaultCredentials);
            ApiClient client = new(_outputHelper, httpClient);

            // TODO: Split test into elevated vs non-elevated tests and skip
            // when not running in the corresponding context. Possibly unelevate
            // dotnet-monitor when running tests elevated.
            if (EnvironmentInformation.IsElevated)
            {
                var statusCodeException = await Assert.ThrowsAsync<ApiStatusCodeException>(
                    () => client.GetProcessesAsync(DefaultTimeout));
                Assert.Equal(HttpStatusCode.Forbidden, statusCodeException.StatusCode);
            }
            else
            {
                // Check that /processes does not challenge for authentication
                var processes = await client.GetProcessesAsync(DefaultTimeout);
                Assert.NotNull(processes);
            }
        }

        private static byte[] GenerateApiKey()
        {
            byte[] apiKey = new byte[32]; // 256 bits

            using RNGCryptoServiceProvider rngProvider = new();
            rngProvider.GetBytes(apiKey);

            return apiKey;
        }
    }
}
