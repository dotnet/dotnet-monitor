﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Diagnostics.Monitoring.UnitTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.UnitTests.Options;
using Microsoft.Diagnostics.Monitoring.UnitTests.Runners;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.UnitTests
{
    public class MetricsTests
    {
        private static readonly TimeSpan DefaultAddressTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan DefaultApiTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan DefaultStartTimeout = TimeSpan.FromSeconds(30);

        private readonly ITestOutputHelper _outputHelper;

        public MetricsTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Tests that turning off metrics via the command line will have the /metrics route not serve metrics.
        /// </summary>
        [Fact]
        public async Task DisableMetricsViaCommandLineTest()
        {
            await using DotNetMonitorRunner toolRunner = new(_outputHelper);
            toolRunner.DisableMetricsViaCommandLine = true;
            await toolRunner.StartAsync(DefaultStartTimeout);

            using ApiClient client = new(_outputHelper, await toolRunner.GetDefaultAddressAsync(DefaultAddressTimeout));

            // Check that /metrics does not serve metrics
            var validationProblemDetailsException = await Assert.ThrowsAsync<ValidationProblemDetailsException>(
                () => client.GetMetricsAsync(DefaultApiTimeout));
            Assert.Equal(HttpStatusCode.BadRequest, validationProblemDetailsException.StatusCode);
            Assert.Equal(StatusCodes.Status400BadRequest, validationProblemDetailsException.Details.Status);
        }

        /// <summary>
        /// Tests that turning off metrics via configuration will have the /metrics route not serve metrics.
        /// </summary>
        [Fact]
        public async Task DisableMetricsViaEnvironmentTest()
        {
            await using DotNetMonitorRunner toolRunner = new(_outputHelper);
            toolRunner.ConfigurationFromEnvironment.Metrics = new()
            {
                Enabled = false
            };
            await toolRunner.StartAsync(DefaultStartTimeout);

            using ApiClient client = new(_outputHelper, await toolRunner.GetDefaultAddressAsync(DefaultAddressTimeout));

            // Check that /metrics does not serve metrics
            var validationProblemDetailsException = await Assert.ThrowsAsync<ValidationProblemDetailsException>(
                () => client.GetMetricsAsync(DefaultApiTimeout));
            Assert.Equal(HttpStatusCode.BadRequest, validationProblemDetailsException.StatusCode);
            Assert.Equal(StatusCodes.Status400BadRequest, validationProblemDetailsException.Details.Status);
        }

        /// <summary>
        /// Tests that turning off metrics via settings will have the /metrics route not serve metrics.
        /// </summary>
        [Fact]
        public async Task DisableMetricsViaSettingsTest()
        {
            await using DotNetMonitorRunner toolRunner = new(_outputHelper);

            await toolRunner.WriteUserSettingsAsync(new RootOptions()
            {
                Metrics = new MetricsOptions()
                {
                    Enabled = false
                }
            }, Timeout.InfiniteTimeSpan);

            await toolRunner.StartAsync(DefaultStartTimeout);

            using ApiClient client = new(_outputHelper, await toolRunner.GetDefaultAddressAsync(DefaultAddressTimeout));

            // Check that /metrics does not serve metrics
            var validationProblemDetailsException = await Assert.ThrowsAsync<ValidationProblemDetailsException>(
                () => client.GetMetricsAsync(DefaultApiTimeout));
            Assert.Equal(HttpStatusCode.BadRequest, validationProblemDetailsException.StatusCode);
            Assert.Equal(StatusCodes.Status400BadRequest, validationProblemDetailsException.Details.Status);
        }

        /// <summary>
        /// Tests that turning off metrics via key-per-file will have the /metrics route not serve metrics.
        /// </summary>
        [Fact]
        public async Task DisableMetricsViaKeyPerFileTest()
        {
            await using DotNetMonitorRunner toolRunner = new(_outputHelper);

            toolRunner.WriteKeyPerValueConfiguration(new RootOptions()
            {
                Metrics = new MetricsOptions()
                {
                    Enabled = false
                }
            });

            await toolRunner.StartAsync(DefaultStartTimeout);

            using ApiClient client = new(_outputHelper, await toolRunner.GetDefaultAddressAsync(DefaultAddressTimeout));

            // Check that /metrics does not serve metrics
            var validationProblemDetailsException = await Assert.ThrowsAsync<ValidationProblemDetailsException>(
                () => client.GetMetricsAsync(DefaultApiTimeout));
            Assert.Equal(HttpStatusCode.BadRequest, validationProblemDetailsException.StatusCode);
            Assert.Equal(StatusCodes.Status400BadRequest, validationProblemDetailsException.Details.Status);
        }
    }
}
