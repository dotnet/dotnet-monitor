// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
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

            using HttpResponseMessage response = await _httpClient.SendAsync(request, token);

            WriteRequestMessage(request);
            WriteResponseMessage(response);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    ValidateContentType(response, ContentTypes.ApplicationJson);
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
            using CancellationTokenSource timeoutSource = new(timeout);
            return await GetProcessesAsync(timeoutSource.Token);
        }

        /// <summary>
        /// GET /metrics
        /// </summary>
        public async Task<string> GetMetricsAsync(CancellationToken token)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, "/metrics");
            request.Headers.Add(HeaderNames.Accept, ContentTypes.TextPlain);

            using HttpResponseMessage response = await _httpClient.SendAsync(request, token);

            WriteRequestMessage(request);
            WriteResponseMessage(response);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    ValidateContentType(response, ContentTypes.TextPlain);
                    return await response.Content.ReadAsStringAsync();
                case HttpStatusCode.BadRequest:
                    ValidateContentType(response, ContentTypes.ApplicationProblemJson);
                    throw await CreateValidationProblemDetailsExceptionAsync(response);
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
            using CancellationTokenSource timeoutSource = new(timeout);
            return await GetMetricsAsync(timeoutSource.Token);
        }

        private static async Task<T> ReadContentAsync<T>(HttpResponseMessage responseMessage)
        {
            using Stream contentStream = await responseMessage.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<T>(contentStream);
        }

        private static Task<List<T>> ReadContentEnumerableAsync<T>(HttpResponseMessage responseMessage)
        {
            return ReadContentAsync<List<T>>(responseMessage);
        }

        private static Exception CreateUnexpectedStatusCodeException(HttpStatusCode statusCode)
        {
            return new ApiStatusCodeException($"Unexpected status code {statusCode}", statusCode);
        }

        private static async Task<ValidationProblemDetailsException> CreateValidationProblemDetailsExceptionAsync(HttpResponseMessage responseMessage)
        {
            return new ValidationProblemDetailsException(
                await ReadContentAsync<ValidationProblemDetails>(responseMessage),
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
