// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Diagnostics.Monitoring.UnitTests.Models;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

            using HttpResponseMessage response = await SendAndLogAsync(
                request,
                HttpCompletionOption.ResponseContentRead,
                token).ConfigureAwait(false);

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
        /// Get /process?pid={pid}
        /// </summary>
        public Task<Models.ProcessInfo> GetProcessAsync(int pid, CancellationToken token)
        {
            return GetProcessAsync(pid.ToString(CultureInfo.InvariantCulture), ProcessIdentifierFormat.PID, token);
        }

        /// <summary>
        /// Get /process?uid={uid}
        /// </summary>
        public Task<Models.ProcessInfo> GetProcessAsync(Guid uid, CancellationToken token)
        {
            return GetProcessAsync(uid.ToString("D"), ProcessIdentifierFormat.UID, token);
        }

        private async Task<Models.ProcessInfo> GetProcessAsync(string processIdentifier, ProcessIdentifierFormat processIdentifierFormat, CancellationToken token)
        {
            string requestPrefix = GetRequestPrefix(processIdentifierFormat);

            string processIdentifierQuery = requestPrefix + processIdentifier;

            using HttpRequestMessage request = new(HttpMethod.Get, $"/process?" + processIdentifierQuery);
            request.Headers.Add(HeaderNames.Accept, ContentTypes.ApplicationJson);

            using HttpResponseMessage response = await SendAndLogAsync(
                request,
                HttpCompletionOption.ResponseContentRead,
                token).ConfigureAwait(false);

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
        /// Get /env?pid={pid}
        /// </summary>
        public Task<Dictionary<string, string>> GetProcessEnvironmentAsync(int pid, CancellationToken token)
        {
            return GetProcessEnvironmentAsync(pid.ToString(CultureInfo.InvariantCulture), ProcessIdentifierFormat.PID, token);
        }

        /// <summary>
        /// Get /env?uid={uid}
        /// </summary>
        public Task<Dictionary<string, string>> GetProcessEnvironmentAsync(Guid uid, CancellationToken token)
        {
            return GetProcessEnvironmentAsync(uid.ToString("D"), ProcessIdentifierFormat.UID, token);
        }

        private async Task<Dictionary<string, string>> GetProcessEnvironmentAsync(string processIdentifier, ProcessIdentifierFormat processIdentifierFormat, CancellationToken token)
        {
            string requestPrefix = GetRequestPrefix(processIdentifierFormat);

            string processIdentifierQuery = requestPrefix + processIdentifier;

            using HttpRequestMessage request = new(HttpMethod.Get, $"/env?" + processIdentifierQuery);
            request.Headers.Add(HeaderNames.Accept, ContentTypes.ApplicationJson);

            using HttpResponseMessage response = await SendAndLogAsync(
                request,
                HttpCompletionOption.ResponseContentRead,
                token).ConfigureAwait(false);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    ValidateContentType(response, ContentTypes.ApplicationJson);
                    return await ReadContentAsync<Dictionary<string, string>>(response).ConfigureAwait(false);
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
        /// Get /dump?pid={pid}&type={dumpType}
        /// </summary>
        public Task<ResponseStreamHolder> CaptureDumpAsync(int pid, DumpType dumpType, CancellationToken token)
        {
            return CaptureDumpAsync(pid.ToString(CultureInfo.InvariantCulture), dumpType, ProcessIdentifierFormat.PID, token);
        }

        /// <summary>
        /// Get /dump?uid={uid}&type={dumpType}
        /// </summary>
        public Task<ResponseStreamHolder> CaptureDumpAsync(Guid uid, DumpType dumpType, CancellationToken token)
        {
            return CaptureDumpAsync(uid.ToString("D"), dumpType, ProcessIdentifierFormat.UID, token);
        }

        private async Task<ResponseStreamHolder> CaptureDumpAsync(string processIdentifier, DumpType dumpType, ProcessIdentifierFormat processIdentifierFormat, CancellationToken token)
        {
            string requestPrefix = GetRequestPrefix(processIdentifierFormat);

            string processIdentifierQuery = requestPrefix + processIdentifier;

            using HttpRequestMessage request = new(HttpMethod.Get, $"/dump?{processIdentifierQuery}&type={dumpType.ToString("G")}");
            request.Headers.Add(HeaderNames.Accept, ContentTypes.ApplicationOctetStream);

            using DisposableBox<HttpResponseMessage> responseBox = new(
                await SendAndLogAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    token).ConfigureAwait(false));

            switch (responseBox.Value.StatusCode)
            {
                case HttpStatusCode.OK:
                    ValidateContentType(responseBox.Value, ContentTypes.ApplicationOctetStream);
                    return await ResponseStreamHolder.CreateAsync(responseBox).ConfigureAwait(false);
                case HttpStatusCode.BadRequest:
                    ValidateContentType(responseBox.Value, ContentTypes.ApplicationProblemJson);
                    throw await CreateValidationProblemDetailsExceptionAsync(responseBox.Value).ConfigureAwait(false);
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.NotFound:
                case HttpStatusCode.TooManyRequests:
                    ThrowIfNotSuccess(responseBox.Value);
                    break;
            }

            throw await CreateUnexpectedStatusCodeExceptionAsync(responseBox.Value).ConfigureAwait(false);
        }

        /// <summary>
        /// GET /logs?pid={pid}&level={logLevel}&durationSeconds={duration}
        /// </summary>
        public Task<ResponseStreamHolder> CaptureLogsAsync(int pid, TimeSpan duration, LogLevel? logLevel, LogFormat logFormat, CancellationToken token)
        {
            return CaptureLogsAsync(pid.ToString(CultureInfo.InvariantCulture), duration, logLevel, logFormat, ProcessIdentifierFormat.PID, token);
        }

        /// <summary>
        /// GET /logs?uid={uid}&level={logLevel}&durationSeconds={duration}
        /// </summary>
        public Task<ResponseStreamHolder> CaptureLogsAsync(Guid uid, TimeSpan duration, LogLevel? logLevel, LogFormat logFormat, CancellationToken token)
        {
            return CaptureLogsAsync(uid.ToString("D"), duration, logLevel, logFormat, ProcessIdentifierFormat.UID, token);
        }

        private Task<ResponseStreamHolder> CaptureLogsAsync(string processIdentifier, TimeSpan duration, LogLevel? logLevel, LogFormat logFormat, ProcessIdentifierFormat processIdentifierFormat, CancellationToken token)
        {
            string requestPrefix = GetRequestPrefix(processIdentifierFormat);

            string processIdentifierQuery = requestPrefix + processIdentifier;

            return CaptureLogsAsync(
                HttpMethod.Get,
                CreateLogsUriString(processIdentifierQuery, duration, logLevel),
                content: null,
                logFormat,
                token);
        }

        /// <summary>
        /// POST /logs?pid={pid}&durationSeconds={duration}
        /// </summary>
        public Task<ResponseStreamHolder> CaptureLogsAsync(int pid, TimeSpan duration, LogsConfiguration configuration, LogFormat logFormat, CancellationToken token)
        {
            return CaptureLogsAsync(pid.ToString(CultureInfo.InvariantCulture), duration, configuration, logFormat, ProcessIdentifierFormat.PID, token);
        }

        /// <summary>
        /// POST /logs?uid={uid}&durationSeconds={duration}
        /// </summary>
        public Task<ResponseStreamHolder> CaptureLogsAsync(Guid uid, TimeSpan duration, LogsConfiguration configuration, LogFormat logFormat, CancellationToken token)
        {
            return CaptureLogsAsync(uid.ToString("D"), duration, configuration, logFormat, ProcessIdentifierFormat.UID, token);
        }

        private Task<ResponseStreamHolder> CaptureLogsAsync(string processIdentifier, TimeSpan duration, LogsConfiguration configuration, LogFormat logFormat, ProcessIdentifierFormat processIdentifierFormat, CancellationToken token)
        {
            string requestPrefix = GetRequestPrefix(processIdentifierFormat);

            string processIdentifierQuery = requestPrefix + processIdentifier;

            JsonSerializerOptions options = new();
            options.Converters.Add(new JsonStringEnumConverter());
            string json = JsonSerializer.Serialize(configuration, options);

            return CaptureLogsAsync(
                HttpMethod.Post,
                CreateLogsUriString(processIdentifierQuery, duration, logLevel: null),
                new StringContent(json, Encoding.UTF8, ContentTypes.ApplicationJson),
                logFormat,
                token);
        }

        private async Task<ResponseStreamHolder> CaptureLogsAsync(HttpMethod method, string uri, HttpContent content, LogFormat logFormat, CancellationToken token)
        {
            string contentType = "";

            if (logFormat == LogFormat.JsonSequence)
            {
                contentType = ContentTypes.ApplicationJsonSequence;
            }
            else if (logFormat == LogFormat.NDJson)
            {
                contentType = ContentTypes.ApplicationNDJson;
            }

            using HttpRequestMessage request = new(method, uri);
            request.Headers.Add(HeaderNames.Accept, contentType);
            request.Content = content;

            using DisposableBox<HttpResponseMessage> responseBox = new(
                await SendAndLogAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    token).ConfigureAwait(false));

            switch (responseBox.Value.StatusCode)
            {
                case HttpStatusCode.OK:
                    ValidateContentType(responseBox.Value, contentType);
                    return await ResponseStreamHolder.CreateAsync(responseBox).ConfigureAwait(false);
                case HttpStatusCode.BadRequest:
                    ValidateContentType(responseBox.Value, ContentTypes.ApplicationProblemJson);
                    throw await CreateValidationProblemDetailsExceptionAsync(responseBox.Value).ConfigureAwait(false);
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.NotFound:
                case HttpStatusCode.TooManyRequests:
                    ThrowIfNotSuccess(responseBox.Value);
                    break;
            }

            throw await CreateUnexpectedStatusCodeExceptionAsync(responseBox.Value).ConfigureAwait(false);
        }

        /// <summary>
        /// GET /metrics
        /// </summary>
        public async Task<string> GetMetricsAsync(CancellationToken token)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, "/metrics");
            request.Headers.Add(HeaderNames.Accept, ContentTypes.TextPlain);

            using HttpResponseMessage response = await SendAndLogAsync(request, HttpCompletionOption.ResponseContentRead, token).ConfigureAwait(false);

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

        private static async Task<T> ReadContentAsync<T>(HttpResponseMessage responseMessage)
        {
            using Stream contentStream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<T>(contentStream).ConfigureAwait(false);
        }

        private static Task<List<T>> ReadContentEnumerableAsync<T>(HttpResponseMessage responseMessage)
        {
            return ReadContentAsync<List<T>>(responseMessage);
        }

        private async Task<HttpResponseMessage> SendAndLogAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken token)
        {
            Stopwatch sw = Stopwatch.StartNew();
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, completionOption, token).ConfigureAwait(false);
                sw.Stop();
            }
            finally
            {
                _outputHelper.WriteLine("-> {0}", request.ToString());
            }

            _outputHelper.WriteLine("<- {0}", response.ToString());
            _outputHelper.WriteLine($"Request duration: {sw.ElapsedMilliseconds} ms");

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

        private static string CreateLogsUriString(string processIdentifierQuery, TimeSpan duration, LogLevel? logLevel)
        {
            StringBuilder routeBuilder = new();
            routeBuilder.Append("/logs?");
            routeBuilder.Append(processIdentifierQuery);
            routeBuilder.Append("&");
            AppendDuration(routeBuilder, duration);
            if (logLevel.HasValue)
            {
                routeBuilder.Append("&level=");
                routeBuilder.Append(logLevel.Value.ToString("G"));
            }
            return routeBuilder.ToString();
        }

        private static void AppendDuration(StringBuilder builder, TimeSpan duration)
        {
            builder.Append("durationSeconds=");
            if (Timeout.InfiniteTimeSpan == duration)
            {
                builder.Append("-1");
            }
            else
            {
                builder.Append(Convert.ToInt32(duration.TotalSeconds).ToString(CultureInfo.InvariantCulture));
            }
        }

        private string GetRequestPrefix(ProcessIdentifierFormat processIdentifierFormat)
        {
            string requestPrefix = "";

            if (processIdentifierFormat == ProcessIdentifierFormat.PID)
            {
                requestPrefix = "pid=";
            }
            else if (processIdentifierFormat == ProcessIdentifierFormat.UID)
            {
                requestPrefix = "uid=";
            }
            else if (processIdentifierFormat == ProcessIdentifierFormat.Name)
            {
                requestPrefix = "name=";
            }

            return requestPrefix;
        }
    }
}
