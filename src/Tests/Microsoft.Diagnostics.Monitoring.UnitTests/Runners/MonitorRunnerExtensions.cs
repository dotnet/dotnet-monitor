// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTests.Runners
{
    internal static class MonitorRunnerExtensions
    {
        /// <summary>
        /// Creates a <see cref="HttpClient"/> over the default address of the <paramref name="runner"/>.
        /// </summary>
        public static Task<HttpClient> CreateHttpClientDefaultAddressAsync(this MonitorRunner runner, IHttpClientFactory factory, TimeSpan timeout)
        {
            return CreateHttpClientDefaultAddressAsync(runner, factory, timeout, Extensions.Options.Options.DefaultName);
        }

        /// <summary>
        /// Creates a named <see cref="HttpClient"/> over the default address of the <paramref name="runner"/>.
        /// </summary>
        public static async Task<HttpClient> CreateHttpClientDefaultAddressAsync(this MonitorRunner runner, IHttpClientFactory factory, TimeSpan timeout, string name)
        {
            HttpClient client = factory.CreateClient(name);

            using CancellationTokenSource cancellation = new(timeout);
            client.BaseAddress = new Uri(await runner.GetDefaultAddressAsync(cancellation.Token), UriKind.Absolute);

            return client;
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
    }
}
