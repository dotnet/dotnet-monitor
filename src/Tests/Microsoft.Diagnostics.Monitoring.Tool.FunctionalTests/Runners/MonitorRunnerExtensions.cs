// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners
{
    internal static class MonitorRunnerExtensions
    {
        /// <summary>
        /// Creates a <see cref="HttpClient"/> over the default address of the <paramref name="runner"/>.
        /// </summary>
        public static Task<HttpClient> CreateHttpClientDefaultAddressAsync(this MonitorRunner runner, IHttpClientFactory factory)
        {
            return runner.CreateHttpClientDefaultAddressAsync(factory, Extensions.Options.Options.DefaultName, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// Creates a named <see cref="HttpClient"/> over the default address of the <paramref name="runner"/>.
        /// </summary>
        public static Task<HttpClient> CreateHttpClientDefaultAddressAsync(this MonitorRunner runner, IHttpClientFactory factory, string name)
        {
            return runner.CreateHttpClientDefaultAddressAsync(factory, name, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// Creates a <see cref="HttpClient"/> over the default address of the <paramref name="runner"/>.
        /// </summary>
        public static Task<HttpClient> CreateHttpClientDefaultAddressAsync(this MonitorRunner runner, IHttpClientFactory factory, TimeSpan timeout)
        {
            return runner.CreateHttpClientDefaultAddressAsync(factory, Extensions.Options.Options.DefaultName, timeout);
        }

        /// <summary>
        /// Creates a named <see cref="HttpClient"/> over the default address of the <paramref name="runner"/>.
        /// </summary>
        public static async Task<HttpClient> CreateHttpClientDefaultAddressAsync(this MonitorRunner runner, IHttpClientFactory factory, string name, TimeSpan timeout)
        {
            HttpClient client = factory.CreateClient(name);

            using CancellationTokenSource cancellation = new(timeout);
            client.BaseAddress = new Uri(await runner.GetDefaultAddressAsync(cancellation.Token), UriKind.Absolute);

            if (runner.UseTempApiKey)
            {
                string monitorApiKey = await runner.GetMonitorApiKey(cancellation.Token);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthenticationTests.ApiKeyScheme, monitorApiKey);
            }

            return client;
        }

        /// <summary>
        /// Creates a <see cref="HttpClient"/> over the metrics address of the <paramref name="runner"/>.
        /// </summary>
        public static Task<HttpClient> CreateHttpClientMetricsAddressAsync(this MonitorRunner runner, IHttpClientFactory factory)
        {
            return runner.CreateHttpClientMetricsAddressAsync(factory, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// Creates a <see cref="HttpClient"/> over the metrics address of the <paramref name="runner"/>.
        /// </summary>
        public static async Task<HttpClient> CreateHttpClientMetricsAddressAsync(this MonitorRunner runner, IHttpClientFactory factory, TimeSpan timeout)
        {
            HttpClient client = factory.CreateClient();

            using CancellationTokenSource cancellation = new(timeout);
            client.BaseAddress = new Uri(await runner.GetMetricsAddressAsync(cancellation.Token), UriKind.Absolute);

            return client;
        }

        public static Task StartAsync(this MonitorRunner runner)
        {
            return runner.StartAsync(CommonTestTimeouts.StartProcess);
        }

        public static async Task StartAsync(this MonitorRunner runner, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            await runner.StartAsync(cancellation.Token);
        }
    }
}
