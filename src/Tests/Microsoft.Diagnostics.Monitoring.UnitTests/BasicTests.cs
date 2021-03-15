// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Http;
using Microsoft.Diagnostics.Monitoring.UnitTests.Runners;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.UnitTests
{
    public class BasicTests
    {
        private static readonly TimeSpan DefaultApiTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan DefaultStartTimeout = TimeSpan.FromSeconds(30);

        private readonly ITestOutputHelper _outputHelper;

        public BasicTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Tests the behavior of the default URL address, namely that all routes require
        /// authentication (401 Unauthorized) except for the metrics route (200 OK).
        /// </summary>
        [Fact]
        public async Task DefaultAddressTest()
        {
            await using DotNetMonitorRunner toolRunner = new DotNetMonitorRunner(_outputHelper);
            
            await toolRunner.StartAsync(DefaultStartTimeout);

            using ApiClient client = new ApiClient(await toolRunner.DefaultAddressTask);

            // Any authenticated route on the default address should 401 Unauthenticated

            var apiException = await Assert.ThrowsAsync<ApiException>(
                () => client.GetProcessesAsync(DefaultApiTimeout));
            Assert.Equal(StatusCodes.Status401Unauthorized, apiException.StatusCode);

            // TODO: Verify other routes (e.g. /dump, /trace, /logs) also 401 Unauthenticated

            // Metrics should not throw on the default address

            await client.GetMetricsAsync(DefaultApiTimeout);
        }

        /// <summary>
        /// Tests the behavior of the mertics URL address, namely that all routes will return
        /// 404 Not Found except for the metrics route (200 OK).
        /// </summary>
        [Fact]
        public async Task MetricsAddressTest()
        {
            await using DotNetMonitorRunner toolRunner = new DotNetMonitorRunner(_outputHelper);

            await toolRunner.StartAsync(DefaultStartTimeout);

            using ApiClient client = new ApiClient(await toolRunner.MetricsAddressTask);

            // Any non-metrics route on the metrics address should 404 Not Found
            var apiException = await Assert.ThrowsAsync<ApiException>(
                () => client.GetProcessesAsync(DefaultApiTimeout));
            Assert.Equal(StatusCodes.Status404NotFound, apiException.StatusCode);

            // TODO: Verify other routes (e.g. /dump, /trace, /logs) also 404 Not Found

            // Metrics should not throw on the metrics address
            await client.GetMetricsAsync(DefaultApiTimeout);
        }

        /// <summary>
        /// Tests that turning off authentication allows access to all routes without authentication.
        /// </summary>
        [Fact]
        public async Task DisableAuthenticationTest()
        {
            await using DotNetMonitorRunner toolRunner = new DotNetMonitorRunner(_outputHelper);
            toolRunner.DisableAuthentication = true;
            await toolRunner.StartAsync(DefaultStartTimeout);

            using ApiClient client = new ApiClient(await toolRunner.DefaultAddressTask);

            // Check that /processes does not challenge for authentication
            var processes = await client.GetProcessesAsync(DefaultApiTimeout);
            Assert.NotNull(processes);

            // Check that /metrics does not challenge for authentication
            await client.GetMetricsAsync(DefaultApiTimeout);
        }

        /// <summary>
        /// Tests that turning off metrics via the command line will have the /metrics route not serve metrics.
        /// </summary>
        [Fact]
        public async Task DisableMetricsViaCommandLineTest()
        {
            await using DotNetMonitorRunner toolRunner = new DotNetMonitorRunner(_outputHelper);
            toolRunner.DisableMetricsViaCommandLine = true;
            await toolRunner.StartAsync(DefaultStartTimeout);

            using ApiClient client = new ApiClient(await toolRunner.DefaultAddressTask);

            // Check that /metrics does not serve metrics
            var apiException = await Assert.ThrowsAsync<ApiException<ValidationProblemDetails>>(
                () => client.GetMetricsAsync(DefaultApiTimeout));
            Assert.Equal(StatusCodes.Status400BadRequest, apiException.StatusCode);
            Assert.Equal(StatusCodes.Status400BadRequest, apiException.Result.Status);
        }
    }
}
