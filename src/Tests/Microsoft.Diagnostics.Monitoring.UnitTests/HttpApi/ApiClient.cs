// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.UnitTests.HttpApi
{
    internal sealed class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ITestOutputHelper _outputHelper;


        public ApiClient(ITestOutputHelper outputHelper, HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
        }

        /// <summary>
        /// GET /processes
        /// </summary>
        public async Task<IEnumerable<Models.ProcessIdentifier>> GetProcessesAsync(CancellationToken token)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, "/processes");
            request.Headers.Add(HeaderNames.Accept, ContentTypes.ApplicationJson);

            using HttpResponseMessage response = await SendAndLogAsync(request, token).ConfigureAwait(false);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    ValidateContentType(response, ContentTypes.ApplicationJson);
                    return await ReadContentEnumerableAsync<Models.ProcessIdentifier>(response).ConfigureAwait(false);
                case HttpStatusCode.BadRequest:
                    ValidateContentType(response, ContentTypes.ApplicationProblemJson);
                    throw await CreateValidationProblemDetailsExceptionAsync(response).ConfigureAwait(false);
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.NotFound:
                    ThrowIfNotSuccess(response);
                    break;
            }

            throw await CreateUnexpectedStatusCodeExceptionAsync(response).ConfigureAwait(false);
        }

        /// <summary>
        /// GET /processes
        /// </summary>
        public async Task<IEnumerable<Models.ProcessIdentifier>> GetProcessesAsync(TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await GetProcessesAsync(timeoutSource.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Get /processes/{pid}
        /// </summary>
        public Task<Models.ProcessInfo> GetProcessAsync(int pid, CancellationToken token)
        {
            return GetProcessAsync(pid.ToString(CultureInfo.InvariantCulture), token);
        }

        /// <summary>
        /// Get /processes/{pid}
        /// </summary>
        public async Task<Models.ProcessInfo> GetProcessAsync(int pid, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await GetProcessAsync(pid, timeoutSource.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Get /processes/{uid}
        /// </summary>
        public Task<Models.ProcessInfo> GetProcessAsync(Guid uid, CancellationToken token)
        {
            return GetProcessAsync(uid.ToString("D"), token);
        }

        /// <summary>
        /// Get /processes/{uid}
        /// </summary>
        public async Task<Models.ProcessInfo> GetProcessAsync(Guid uid, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await GetProcessAsync(uid, timeoutSource.Token).ConfigureAwait(false);
        }

        private async Task<Models.ProcessInfo> GetProcessAsync(string processKey, CancellationToken token)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, $"/processes/{processKey}");
            request.Headers.Add(HeaderNames.Accept, ContentTypes.ApplicationJson);

            using HttpResponseMessage response = await SendAndLogAsync(request, token).ConfigureAwait(false);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    ValidateContentType(response, ContentTypes.ApplicationJson);
                    return await ReadContentAsync<Models.ProcessInfo>(response).ConfigureAwait(false);
                case HttpStatusCode.BadRequest:
                    ValidateContentType(response, ContentTypes.ApplicationProblemJson);
                    throw await CreateValidationProblemDetailsExceptionAsync(response).ConfigureAwait(false);
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.NotFound:
                    ThrowIfNotSuccess(response);
                    break;
            }

            throw await CreateUnexpectedStatusCodeExceptionAsync(response).ConfigureAwait(false);
        }

        /// <summary>
        /// GET /metrics
        /// </summary>
        public async Task<string> GetMetricsAsync(CancellationToken token)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, "/metrics");
            request.Headers.Add(HeaderNames.Accept, ContentTypes.TextPlain);

            using HttpResponseMessage response = await SendAndLogAsync(request, token).ConfigureAwait(false);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    ValidateContentType(response, ContentTypes.TextPlain);
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                case HttpStatusCode.BadRequest:
                    ValidateContentType(response, ContentTypes.ApplicationProblemJson);
                    throw await CreateValidationProblemDetailsExceptionAsync(response).ConfigureAwait(false);
                case HttpStatusCode.Unauthorized:
                    ThrowIfNotSuccess(response);
                    break;
            }

            throw await CreateUnexpectedStatusCodeExceptionAsync(response).ConfigureAwait(false);
        }

        /// <summary>
        /// GET /metrics
        /// </summary>
        public async Task<string> GetMetricsAsync(TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            return await GetMetricsAsync(timeoutSource.Token).ConfigureAwait(false);
        }

        private static async Task<T> ReadContentAsync<T>(HttpResponseMessage responseMessage)
        {
            using Stream contentStream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<T>(contentStream).ConfigureAwait(false);
        }

        private static Task<List<T>> ReadContentEnumerableAsync<T>(HttpResponseMessage responseMessage)
        {
            return ReadContentAsync<List<T>>(responseMessage);
        }

        private async Task<HttpResponseMessage> SendAndLogAsync(HttpRequestMessage request, CancellationToken token)
        {
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, token).ConfigureAwait(false);
            }
            finally
            {
                _outputHelper.WriteLine("-> {0}", request.ToString());
            }

            _outputHelper.WriteLine("<- {0}", response.ToString());

            return response;
        }

        private static async Task<Exception> CreateUnexpectedStatusCodeExceptionAsync(HttpResponseMessage response)
        {
            string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return new ApiStatusCodeException($"Unexpected status code {response.StatusCode}. Response content: {content}", response.StatusCode);
        }

        private static async Task<ValidationProblemDetailsException> CreateValidationProblemDetailsExceptionAsync(HttpResponseMessage responseMessage)
        {
            return new ValidationProblemDetailsException(
                await ReadContentAsync<ValidationProblemDetails>(responseMessage).ConfigureAwait(false),
                responseMessage.StatusCode);
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

        private void ValidateContentType(HttpResponseMessage responseMessage, string expectedMediaType)
        {
            Assert.Equal(expectedMediaType, responseMessage.Content.Headers.ContentType?.MediaType);
        }
    }
}
