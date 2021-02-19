// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.UnitTests.HttpApi
{
    internal sealed class ApiClient : IDisposable
    {
        private readonly string _baseUrl;
        private readonly bool _disposeHttpClient;
        private readonly HttpClient _httpClient;
        private readonly ITestOutputHelper _outputHelper;

        public ApiClient(ITestOutputHelper outputHelper, string baseUrl)
            : this(outputHelper, baseUrl, new HttpClient())
        {
            _disposeHttpClient = true;
        }

        public ApiClient(ITestOutputHelper outputHelper, string baseUrl, HttpClient httpClient)
        {
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
        }

        public void Dispose()
        {
            if (_disposeHttpClient)
            {
                _httpClient.Dispose();
            }
        }

        /// <summary>
        /// GET /processes
        /// </summary>
        public async Task<IEnumerable<Models.ProcessIdentifier>> GetProcessesAsync(CancellationToken token)
        {
            Uri uri = new Uri($"{_baseUrl}/processes", UriKind.Absolute);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Add(HeaderNames.Accept, ContentTypes.ApplicationJson);

            WriteRequestMessage(request);

            using HttpResponseMessage response = await _httpClient.SendAsync(request, token);

            WriteResponseMessage(response);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    return await ReadContentEnumerableAsync<Models.ProcessIdentifier>(response);
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.NotFound:
                    ThrowIfNotSuccess(response);
                    break;
            }

            throw CreateUnexpectedStatusCodeException(response.StatusCode);
        }

        /// <summary>
        /// GET /processes
        /// </summary>
        public async Task<IEnumerable<Models.ProcessIdentifier>> GetProcessesAsync(TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new CancellationTokenSource(timeout);
            return await GetProcessesAsync(timeoutSource.Token);
        }

        /// <summary>
        /// GET /metrics
        /// </summary>
        public async Task<string> GetMetricsAsync(CancellationToken token)
        {
            Uri uri = new Uri($"{_baseUrl}/metrics", UriKind.Absolute);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Add(HeaderNames.Accept, ContentTypes.TextPlain);

            WriteRequestMessage(request);

            using HttpResponseMessage response = await _httpClient.SendAsync(request, token);

            WriteResponseMessage(response);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    return await response.Content.ReadAsStringAsync();
                case HttpStatusCode.Unauthorized:
                    ThrowIfNotSuccess(response);
                    break;
            }

            throw CreateUnexpectedStatusCodeException(response.StatusCode);
        }

        /// <summary>
        /// GET /metrics
        /// </summary>
        public async Task<string> GetMetricsAsync(TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new CancellationTokenSource(timeout);
            return await GetMetricsAsync(timeoutSource.Token);
        }

        private static async Task<T> ReadContentAsync<T>(HttpResponseMessage response)
        {
            using Stream contentStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<T>(contentStream);
        }

        private static Task<List<T>> ReadContentEnumerableAsync<T>(HttpResponseMessage response)
        {
            return ReadContentAsync<List<T>>(response);
        }

        private static Exception CreateUnexpectedStatusCodeException(HttpStatusCode statusCode)
        {
            return new ApiStatusCodeException($"Unexpected status code {statusCode}", statusCode);
        }

        private static void ThrowIfNotSuccess(HttpResponseMessage responseMessage)
        {
            try
            {
                responseMessage.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new ApiStatusCodeException(ex.Message, responseMessage.StatusCode);
            }
        }

        private void WriteRequestMessage(HttpRequestMessage requestMessage)
        {
            _outputHelper.WriteLine("-> {0}", requestMessage.ToString());
        }

        private void WriteResponseMessage(HttpResponseMessage responseMessage)
        {
            _outputHelper.WriteLine("<- {0}", responseMessage.ToString());
        }
    }
}
