// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
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

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi
{
    internal sealed class ApiClient
    {
        private static readonly JsonSerializerOptions DefaultJsonDeserializeOptions
            = CreateJsonDeserializeOptions();
        private static readonly JsonSerializerOptions DefaultJsonSerializeOptions
            = CreateJsonSerializeOptions();

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
        public async Task<IEnumerable<ProcessIdentifier>> GetProcessesAsync(CancellationToken token)
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
                    return await ReadContentEnumerableAsync<ProcessIdentifier>(response).ConfigureAwait(false);
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
        /// Capable of getting every combination of process query: PID, UID, and/or Name
        /// Get /process?pid={pid}&uid={uid}&name={name}
        /// </summary>
        public Task<ProcessInfo> GetProcessAsync(int? pid, Guid? uid, string name, CancellationToken token)
        {
            return GetProcessAsync(GetProcessQuery(pid: pid, uid: uid, name: name), token);
        }

        private async Task<ProcessInfo> GetProcessAsync(string processQuery, CancellationToken token)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, $"/process?" + processQuery);
            request.Headers.Add(HeaderNames.Accept, ContentTypes.ApplicationJson);

            using HttpResponseMessage response = await SendAndLogAsync(
                request,
                HttpCompletionOption.ResponseContentRead,
                token).ConfigureAwait(false);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    ValidateContentType(response, ContentTypes.ApplicationJson);
                    return await ReadContentAsync<ProcessInfo>(response).ConfigureAwait(false);
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
            return GetProcessEnvironmentAsync(GetProcessQuery(pid:pid), token);
        }

        /// <summary>
        /// Get /env?uid={uid}
        /// </summary>
        public Task<Dictionary<string, string>> GetProcessEnvironmentAsync(Guid uid, CancellationToken token)
        {
            return GetProcessEnvironmentAsync(GetProcessQuery(uid:uid), token);
        }

        private async Task<Dictionary<string, string>> GetProcessEnvironmentAsync(string processQuery, CancellationToken token)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, $"/env?" + processQuery);
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

        public async Task<DotnetMonitorInfo> GetInfoAsync(CancellationToken token)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, $"/info");
            request.Headers.Add(HeaderNames.Accept, ContentTypes.ApplicationJson);

            using HttpResponseMessage response = await SendAndLogAsync(
                request,
                HttpCompletionOption.ResponseContentRead,
                token).ConfigureAwait(false);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    ValidateContentType(response, ContentTypes.ApplicationJson);
                    return await ReadContentAsync<DotnetMonitorInfo>(response).ConfigureAwait(false);
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
            return CaptureDumpAsync(GetProcessQuery(pid:pid), dumpType, token);
        }

        /// <summary>
        /// Get /dump?uid={uid}&type={dumpType}
        /// </summary>
        public Task<ResponseStreamHolder> CaptureDumpAsync(Guid uid, DumpType dumpType, CancellationToken token)
        {
            return CaptureDumpAsync(GetProcessQuery(uid:uid), dumpType, token);
        }

        private async Task<ResponseStreamHolder> CaptureDumpAsync(string processQuery, DumpType dumpType, CancellationToken token)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, $"/dump?{processQuery}&type={dumpType.ToString("G")}");
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
            return CaptureLogsAsync(GetProcessQuery(pid:pid), duration, logLevel, logFormat, token);
        }

        /// <summary>
        /// GET /logs?uid={uid}&level={logLevel}&durationSeconds={duration}
        /// </summary>
        public Task<ResponseStreamHolder> CaptureLogsAsync(Guid uid, TimeSpan duration, LogLevel? logLevel, LogFormat logFormat, CancellationToken token)
        {
            return CaptureLogsAsync(GetProcessQuery(uid:uid), duration, logLevel, logFormat, token);
        }

        private Task<ResponseStreamHolder> CaptureLogsAsync(string processQuery, TimeSpan duration, LogLevel? logLevel, LogFormat logFormat, CancellationToken token)
        {
            return CaptureLogsAsync(
                HttpMethod.Get,
                CreateLogsUriString(processQuery, duration, logLevel),
                content: null,
                logFormat,
                token);
        }

        /// <summary>
        /// POST /logs?pid={pid}&durationSeconds={duration}
        /// </summary>
        public Task<ResponseStreamHolder> CaptureLogsAsync(int pid, TimeSpan duration, LogsConfiguration configuration, LogFormat logFormat, CancellationToken token)
        {
            return CaptureLogsAsync(GetProcessQuery(pid:pid), duration, configuration, logFormat, token);
        }

        private Task<ResponseStreamHolder> CaptureLogsAsync(string processQuery, TimeSpan duration, LogsConfiguration configuration, LogFormat logFormat, CancellationToken token)
        {
            string json = JsonSerializer.Serialize(configuration, DefaultJsonSerializeOptions);

            return CaptureLogsAsync(
                HttpMethod.Post,
                CreateLogsUriString(processQuery, duration, logLevel: null),
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

        public Task<ResponseStreamHolder> CaptureLiveMetricsAsync(int processId, int durationSeconds, int refreshInterval, CancellationToken token)
        {
            return CaptureLiveMetricsAsync(processId, durationSeconds, refreshInterval, HttpMethod.Get, content: null, token: token);
        }

        public Task<ResponseStreamHolder> CaptureLiveMetricsAsync(int processId, int durationSeconds, int refreshInterval, EventMetricsConfiguration metricsConfiguration, CancellationToken token)
        {
            string content = JsonSerializer.Serialize(metricsConfiguration, DefaultJsonSerializeOptions);

            return CaptureLiveMetricsAsync(processId,
                durationSeconds,
                refreshInterval,
                HttpMethod.Post,
                new StringContent(content, Encoding.UTF8, ContentTypes.ApplicationJson),
                token);
        }

        private async Task<ResponseStreamHolder> CaptureLiveMetricsAsync(int processId, int durationSeconds, int refreshInterval, HttpMethod method, HttpContent content, CancellationToken token)
        {
            string uri = FormattableString.Invariant($"/livemetrics?pid={processId}&durationSeconds={durationSeconds}&metricsIntervalSeconds={refreshInterval}");

            using HttpRequestMessage request = new(method, uri);
            request.Headers.Add(HeaderNames.Accept, ContentTypes.ApplicationJsonSequence);
            request.Content = content;

            using DisposableBox<HttpResponseMessage> responseBox = new(
                await SendAndLogAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    token).ConfigureAwait(false));

            switch (responseBox.Value.StatusCode)
            {
                case HttpStatusCode.OK:
                    ValidateContentType(responseBox.Value, ContentTypes.ApplicationJsonSequence);
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

        public async Task<HttpResponseMessage> ApiCall(string routeAndQuery, CancellationToken token)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, routeAndQuery);
            HttpResponseMessage response = await SendAndLogAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            return response;
        }

        public async Task<OperationResponse> EgressTraceAsync(int processId, int durationSeconds, string egressProvider, CancellationToken token)
        {
            string uri = FormattableString.Invariant($"/trace?pid={processId}&egressProvider={egressProvider}&durationSeconds={durationSeconds}");
            using HttpRequestMessage request = new(HttpMethod.Get, uri);
            using HttpResponseMessage response = await SendAndLogAsync(request, HttpCompletionOption.ResponseContentRead, token).ConfigureAwait(false);

            switch (response.StatusCode)
            {
                case HttpStatusCode.Accepted:
                    return new OperationResponse(response.StatusCode, response.Headers.Location);
                case HttpStatusCode.BadRequest:
                case HttpStatusCode.TooManyRequests:
                    ValidateContentType(response, ContentTypes.ApplicationProblemJson);
                    throw await CreateValidationProblemDetailsExceptionAsync(response).ConfigureAwait(false);
                case HttpStatusCode.Unauthorized:
                    ThrowIfNotSuccess(response);
                    break;
            }

            throw await CreateUnexpectedStatusCodeExceptionAsync(response).ConfigureAwait(false);
        }

        public async Task<List<OperationSummary>> GetOperations(CancellationToken token)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, "/operations");
            using HttpResponseMessage response = await SendAndLogAsync(request, HttpCompletionOption.ResponseContentRead, token).ConfigureAwait(false);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    ValidateContentType(response, ContentTypes.ApplicationJson);
                    return await ReadContentEnumerableAsync<OperationSummary>(response).ConfigureAwait(false);
                case HttpStatusCode.BadRequest:
                    ValidateContentType(response, ContentTypes.ApplicationProblemJson);
                    throw await CreateValidationProblemDetailsExceptionAsync(response).ConfigureAwait(false);
                case HttpStatusCode.Unauthorized:
                    ThrowIfNotSuccess(response);
                    break;
            }

            throw await CreateUnexpectedStatusCodeExceptionAsync(response).ConfigureAwait(false);
        }

        public async Task<OperationStatusResponse> GetOperationStatus(Uri operation, CancellationToken token)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, operation.ToString());
            using HttpResponseMessage response = await SendAndLogAsync(request, HttpCompletionOption.ResponseContentRead, token).ConfigureAwait(false);

            switch (response.StatusCode)
            {
                case HttpStatusCode.Created:
                case HttpStatusCode.OK:
                    ValidateContentType(response, ContentTypes.ApplicationJson);
                    return new OperationStatusResponse(response.StatusCode, await ReadContentAsync<OperationStatus>(response).ConfigureAwait(false));
                case HttpStatusCode.BadRequest:
                    ValidateContentType(response, ContentTypes.ApplicationProblemJson);
                    throw await CreateValidationProblemDetailsExceptionAsync(response).ConfigureAwait(false);
                case HttpStatusCode.Unauthorized:
                    ThrowIfNotSuccess(response);
                    break;
            }

            throw await CreateUnexpectedStatusCodeExceptionAsync(response).ConfigureAwait(false);
        }

        public async Task<HttpStatusCode> CancelEgressOperation(Uri operation, CancellationToken token)
        {
            using HttpRequestMessage request = new(HttpMethod.Delete, operation.ToString());
            using HttpResponseMessage response = await SendAndLogAsync(request, HttpCompletionOption.ResponseContentRead, token).ConfigureAwait(false);
            
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    return response.StatusCode;
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
            return await JsonSerializer.DeserializeAsync<T>(contentStream, DefaultJsonDeserializeOptions).ConfigureAwait(false);
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

        private static string GetProcessQuery(int? pid = null, Guid? uid = null, string name = null)
        {
            string assembledQuery = "";

            if (pid != null)
            {
                assembledQuery += "pid=" + pid.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (uid != null)
            {
                if (assembledQuery.Length > 0)
                {
                    assembledQuery += "&";
                }

                assembledQuery += "uid=" + uid.Value.ToString("D");
            }

            if (name != null)
            {
                if (assembledQuery.Length > 0)
                {
                    assembledQuery += "&";
                }

                assembledQuery += "name=" + name;
            }

            if (!assembledQuery.Equals(""))
            {
                return assembledQuery;
            }

            throw new ArgumentException("One of PID, UID, or Name must be specified.");
        }

        private static JsonSerializerOptions CreateJsonDeserializeOptions()
        {
            JsonSerializerOptions options = new();
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }

        private static JsonSerializerOptions CreateJsonSerializeOptions()
        {
            JsonSerializerOptions options = new();
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }
    }
}