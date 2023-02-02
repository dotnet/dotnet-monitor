// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners
{
    internal static class MonitorCollectRunnerExtensions
    {
        /// <summary>
        /// Creates a <see cref="HttpClient"/> over the address of the <paramref name="runner"/>.
        /// </summary>
        public static Task<HttpClient> CreateHttpClientAsync(this MonitorCollectRunner runner, IHttpClientFactory factory, string address)
        {
            return runner.CreateHttpClientAsync(factory, address, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// Creates a <see cref="HttpClient"/> over the address of the <paramref name="runner"/>.
        /// </summary>
        public static async Task<HttpClient> CreateHttpClientAsync(this MonitorCollectRunner runner, IHttpClientFactory factory, string address, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            return await runner.CreateHttpClientAsync(factory, address, Extensions.Options.Options.DefaultName, cancellation.Token, timeout);
        }

        /// <summary>
        /// Creates a named <see cref="HttpClient"/> over the address of the <paramref name="runner"/>.
        /// </summary>
        public static async Task<HttpClient> CreateHttpClientAsync(this MonitorCollectRunner runner, IHttpClientFactory factory, string address, string name, CancellationToken token, TimeSpan? timeout = null)
        {
            HttpClient client = factory.CreateClient(name);
            if (timeout.HasValue)
            {
                client.Timeout = timeout.Value;
            }
            client.BaseAddress = new Uri(address, UriKind.Absolute);

            if (runner.UseTempApiKey)
            {
                string monitorApiKey = await runner.GetMonitorApiKey(token);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthConstants.ApiKeySchema, monitorApiKey);
            }

            return client;
        }

        /// <summary>
        /// Creates a <see cref="HttpClient"/> over the default address of the <paramref name="runner"/>.
        /// </summary>
        public static Task<HttpClient> CreateHttpClientDefaultAddressAsync(this MonitorCollectRunner runner, IHttpClientFactory factory)
        {
            return runner.CreateHttpClientDefaultAddressAsync(factory, Extensions.Options.Options.DefaultName, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// Creates a named <see cref="HttpClient"/> over the default address of the <paramref name="runner"/>.
        /// </summary>
        public static Task<HttpClient> CreateHttpClientDefaultAddressAsync(this MonitorCollectRunner runner, IHttpClientFactory factory, string name)
        {
            return runner.CreateHttpClientDefaultAddressAsync(factory, name, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// Creates a <see cref="HttpClient"/> over the default address of the <paramref name="runner"/>.
        /// </summary>
        public static Task<HttpClient> CreateHttpClientDefaultAddressAsync(this MonitorCollectRunner runner, IHttpClientFactory factory, TimeSpan timeout)
        {
            return runner.CreateHttpClientDefaultAddressAsync(factory, Extensions.Options.Options.DefaultName, timeout);
        }

        /// <summary>
        /// Creates a named <see cref="HttpClient"/> over the default address of the <paramref name="runner"/>.
        /// </summary>
        public static async Task<HttpClient> CreateHttpClientDefaultAddressAsync(this MonitorCollectRunner runner, IHttpClientFactory factory, string name, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);

            string address = await runner.GetDefaultAddressAsync(cancellation.Token);

            return await runner.CreateHttpClientAsync(factory, address, name, cancellation.Token, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// Creates a <see cref="HttpClient"/> over the metrics address of the <paramref name="runner"/>.
        /// </summary>
        public static Task<HttpClient> CreateHttpClientMetricsAddressAsync(this MonitorCollectRunner runner, IHttpClientFactory factory)
        {
            return runner.CreateHttpClientMetricsAddressAsync(factory, TestTimeouts.HttpApi);
        }

        /// <summary>
        /// Creates a <see cref="HttpClient"/> over the metrics address of the <paramref name="runner"/>.
        /// </summary>
        public static async Task<HttpClient> CreateHttpClientMetricsAddressAsync(this MonitorCollectRunner runner, IHttpClientFactory factory, TimeSpan timeout)
        {
            HttpClient client = factory.CreateClient();

            using CancellationTokenSource cancellation = new(timeout);
            client.BaseAddress = new Uri(await runner.GetMetricsAddressAsync(cancellation.Token), UriKind.Absolute);

            return client;
        }

        public static Task StartAsync(this MonitorCollectRunner runner)
        {
            return runner.StartAsync(CommonTestTimeouts.StartProcess);
        }

        public static async Task StartAsync(this MonitorCollectRunner runner, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            await runner.StartAsync(cancellation.Token);
        }

        public static Task StopAsync(this MonitorCollectRunner runner)
        {
            return runner.StopAsync(CommonTestTimeouts.StopProcess);
        }

        public static async Task StopAsync(this MonitorCollectRunner runner, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            await runner.StopAsync(cancellation.Token);
        }

        public static async Task WaitForStartCollectArtifactAsync(this MonitorCollectRunner runner, string artifactType, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            await runner.WaitForStartCollectArtifactAsync(artifactType, cancellation.Token);
        }

        public static Task WaitForStartCollectLogsAsync(this MonitorCollectRunner runner)
        {
            return runner.WaitForStartCollectArtifactAsync(Utils.ArtifactType_Logs, CommonTestTimeouts.LogsTimeout);
        }

        public static Task WaitForCollectionRuleActionsCompletedAsync(this MonitorCollectRunner runner, string ruleName)
        {
            return runner.WaitForCollectionRuleActionsCompletedAsync(ruleName, TestTimeouts.CollectionRuleActionsCompletedTimeout);
        }

        public static async Task WaitForCollectionRuleActionsCompletedAsync(this MonitorCollectRunner runner, string ruleName, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            await runner.WaitForCollectionRuleActionsCompletedAsync(ruleName, cancellation.Token);
        }

        public static Task WaitForCollectionRuleCompleteAsync(this MonitorCollectRunner runner, string ruleName)
        {
            return runner.WaitForCollectionRuleCompleteAsync(ruleName, TestTimeouts.CollectionRuleCompletionTimeout);
        }

        public static async Task WaitForCollectionRuleCompleteAsync(this MonitorCollectRunner runner, string ruleName, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            await runner.WaitForCollectionRuleCompleteAsync(ruleName, cancellation.Token);
        }

        public static Task WaitForCollectionRuleUnmatchedFiltersAsync(this MonitorCollectRunner runner, string ruleName)
        {
            return runner.WaitForCollectionRuleUnmatchedFiltersAsync(ruleName, TestTimeouts.CollectionRuleFilteredTimeout);
        }

        public static async Task WaitForCollectionRuleUnmatchedFiltersAsync(this MonitorCollectRunner runner, string ruleName, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            await runner.WaitForCollectionRuleUnmatchedFiltersAsync(ruleName, cancellation.Token);
        }

        public static Task WaitForCollectionRuleStartedAsync(this MonitorCollectRunner runner, string ruleName)
        {
            return runner.WaitForCollectionRuleStartedAsync(ruleName, TestTimeouts.CollectionRuleCompletionTimeout);
        }

        public static async Task WaitForCollectionRuleStartedAsync(this MonitorCollectRunner runner, string ruleName, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            await runner.WaitForCollectionRuleStartedAsync(ruleName, cancellation.Token);
        }

        public static Task WaitForCollectionRulesStoppedAsync(this MonitorCollectRunner runner)
        {
            return runner.WaitForCollectionRulesStoppedAsync(TestTimeouts.CollectionRuleCompletionTimeout);
        }

        public static async Task WaitForCollectionRulesStoppedAsync(this MonitorCollectRunner runner, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            await runner.WaitForCollectionRulesStoppedAsync(cancellation.Token);
        }
    }
}
