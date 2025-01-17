// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Diagnostics.Monitoring.Options;
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
        private static readonly JsonSerializerOptions ValidationProblemDetailsDeserializeOptions
            = CreateValidationProblemDetailsDeserializeOptions();

        private readonly HttpClient _httpClient;
        private readonly ITestOutputHelper _outputHelper;

        public ApiClient(ITestOutputHelper outputHelper, HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
        }

        public Uri BaseAddress => _httpClient.BaseAddress;

        /// <summary>
        /// GET /
        /// </summary>
        public async Task<HttpResponseMessage> GetRootAsync(CancellationToken token)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, "/");

            using HttpResponseMessage response = await SendAndLogAsync(
                request,
                HttpCompletionOption.ResponseContentRead,
                token).ConfigureAwait(false);

            switch (response.StatusCode)
            {
                case HttpStatusCode.NotFound:
                    ThrowIfNotSuccess(response);
                    break;

                case HttpStatusCode.OK:
                case HttpStatusCode.Redirect:
                case HttpStatusCode.Moved:
                    return response;
            }
            throw await CreateUnexpectedStatusCodeExceptionAsync(response).ConfigureAwait(false);
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
        /// Get <![CDATA[/process?pid={pid}&uid={uid}&name={name}]]>
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
            return GetProcessEnvironmentAsync(GetProcessQuery(pid: pid), token);
        }

        /// <summary>
        /// Get /env?uid={uid}
        /// </summary>
        public Task<Dictionary<string, string>> GetProcessEnvironmentAsync(Guid uid, CancellationToken token)
        {
            return GetProcessEnvironmentAsync(GetProcessQuery(uid: uid), token);
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
        /// Get <![CDATA[/dump?pid={pid}&type={dumpType}]]>
        /// </summary>
        public Task<ResponseStreamHolder> CaptureDumpAsync(int pid, DumpType dumpType, CancellationToken token)
        {
            return CaptureDumpAsync(GetProcessQuery(pid: pid), dumpType, token);
        }

        /// <summary>
        /// Get <![CDATA[/dump?uid={uid}&type={dumpType}]]>
        /// </summary>
        public Task<ResponseStreamHolder> CaptureDumpAsync(Guid uid, DumpType dumpType, CancellationToken token)
        {
            return CaptureDumpAsync(GetProcessQuery(uid: uid), dumpType, token);
        }

        private async Task<ResponseStreamHolder> CaptureDumpAsync(string processQuery, DumpType dumpType, CancellationToken token)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, $"/dump?{processQuery}&type={dumpType:G}");
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
        /// Capable of getting every combination of process query: PID, UID, and/or Name
        /// Get <![CDATA[/collectionrules?pid={pid}&uid={uid}&name={name}]]>
        /// </summary>
        public Task<Dictionary<string, CollectionRuleDescription>> GetCollectionRulesDescriptionAsync(int? pid, Guid? uid, string name, CancellationToken token)
        {
            return GetCollectionRulesDescriptionAsync(GetProcessQuery(pid: pid, uid: uid, name: name), token);
        }

        private async Task<Dictionary<string, CollectionRuleDescription>> GetCollectionRulesDescriptionAsync(string processQuery, CancellationToken token)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, $"/collectionRules?" + processQuery);
            request.Headers.Add(HeaderNames.Accept, ContentTypes.ApplicationJson);

            using HttpResponseMessage response = await SendAndLogAsync(
                request,
                HttpCompletionOption.ResponseContentRead,
                token).ConfigureAwait(false);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    ValidateContentType(response, ContentTypes.ApplicationJson);
                    return await ReadContentAsync<Dictionary<string, CollectionRuleDescription>>(response).ConfigureAwait(false);
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
        /// GET <![CDATA[/collectionrules/{collectionrulename}?pid={pid}&uid={uid}&name={name}]]>
        /// </summary>
        public Task<CollectionRuleDetailedDescription> GetCollectionRuleDetailedDescriptionAsync(string collectionRuleName, int? pid, Guid? uid, string name, CancellationToken token)
        {
            return GetCollectionRuleDetailedDescriptionAsync(collectionRuleName, GetProcessQuery(pid: pid, uid: uid, name: name), token);
        }

        private async Task<CollectionRuleDetailedDescription> GetCollectionRuleDetailedDescriptionAsync(string collectionRuleName, string processQuery, CancellationToken token)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, $"/collectionRules/" + collectionRuleName + "?" + processQuery);
            request.Headers.Add(HeaderNames.Accept, ContentTypes.ApplicationJson);

            using HttpResponseMessage response = await SendAndLogAsync(
                request,
                HttpCompletionOption.ResponseContentRead,
                token).ConfigureAwait(false);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    ValidateContentType(response, ContentTypes.ApplicationJson);
                    return await ReadContentAsync<CollectionRuleDetailedDescription>(response).ConfigureAwait(false);
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
        /// GET <![CDATA[/logs?pid={pid}&level={logLevel}&durationSeconds={duration}]]>
        /// </summary>
        public Task<ResponseStreamHolder> CaptureLogsAsync(int pid, TimeSpan duration, LogLevel? logLevel, LogFormat logFormat, CancellationToken token)
        {
            return CaptureLogsAsync(GetProcessQuery(pid: pid), duration, logLevel, logFormat, token);
        }

        /// <summary>
        /// GET <![CDATA[/logs?uid={uid}&level={logLevel}&durationSeconds={duration}]]>
        /// </summary>
        public Task<ResponseStreamHolder> CaptureLogsAsync(Guid uid, TimeSpan duration, LogLevel? logLevel, LogFormat logFormat, CancellationToken token)
        {
            return CaptureLogsAsync(GetProcessQuery(uid: uid), duration, logLevel, logFormat, token);
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
        /// POST <![CDATA[/logs?pid={pid}&durationSeconds={duration}]]>
        /// </summary>
        public Task<ResponseStreamHolder> CaptureLogsAsync(int pid, TimeSpan duration, LogsConfiguration configuration, LogFormat logFormat, CancellationToken token)
        {
            return CaptureLogsAsync(GetProcessQuery(pid: pid), duration, configuration, logFormat, token);
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
            else if (logFormat == LogFormat.NewlineDelimitedJson)
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
        /// GET <![CDATA[/trace?pid={pid}&profile={profile}&durationSeconds={duration}]]>
        /// </summary>
        public Task<ResponseStreamHolder> CaptureTraceAsync(int pid, TimeSpan duration, TraceProfile? profile, CancellationToken token)
        {
            return CaptureTraceAsync(GetProcessQuery(pid: pid), duration, profile, token);
        }

        private Task<ResponseStreamHolder> CaptureTraceAsync(string processQuery, TimeSpan duration, TraceProfile? profile, CancellationToken token)
        {
            return CaptureTraceAsync(
                HttpMethod.Get,
                CreateTraceUriString(processQuery, duration, profile),
                content: null,
                token);
        }

        private async Task<ResponseStreamHolder> CaptureTraceAsync(HttpMethod method, string uri, HttpContent content, CancellationToken token)
        {
            string contentType = ContentTypes.ApplicationOctetStream;

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
                    return await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                case HttpStatusCode.BadRequest:
                    ValidateContentType(response, ContentTypes.ApplicationProblemJson);
                    throw await CreateValidationProblemDetailsExceptionAsync(response).ConfigureAwait(false);
                case HttpStatusCode.Unauthorized:
                    ThrowIfNotSuccess(response);
                    break;
            }

            throw await CreateUnexpectedStatusCodeExceptionAsync(response).ConfigureAwait(false);
        }

        public Task<ResponseStreamHolder> CaptureMetricsAsync(int processId, int durationSeconds, CancellationToken token)
        {
            return CaptureMetricsAsync(processId, durationSeconds, HttpMethod.Get, content: null, token: token);
        }

        public Task<ResponseStreamHolder> CaptureMetricsAsync(int processId, int durationSeconds, EventMetricsConfiguration metricsConfiguration, CancellationToken token)
        {
            string content = JsonSerializer.Serialize(metricsConfiguration, DefaultJsonSerializeOptions);

            return CaptureMetricsAsync(processId,
                durationSeconds,
                HttpMethod.Post,
                new StringContent(content, Encoding.UTF8, ContentTypes.ApplicationJson),
                token);
        }

        private async Task<ResponseStreamHolder> CaptureMetricsAsync(int processId, int durationSeconds, HttpMethod method, HttpContent content, CancellationToken token)
        {
            string uri = FormattableString.Invariant($"/livemetrics?pid={processId}&durationSeconds={durationSeconds}");

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

        public async Task<ResponseStreamHolder> CaptureExceptionsAsync(ExceptionsConfiguration configuration, int processId, ExceptionFormat format, CancellationToken token)
        {
            string json = JsonSerializer.Serialize(configuration, DefaultJsonSerializeOptions);

            return await CaptureExceptionsAsync(
                HttpMethod.Post,
                new StringContent(json, Encoding.UTF8, ContentTypes.ApplicationJson),
                processId,
                format,
                token);
        }

        public async Task<ResponseStreamHolder> CaptureExceptionsAsync(int processId, ExceptionFormat format, CancellationToken token)
        {
            return await CaptureExceptionsAsync(HttpMethod.Get, null, processId, format, token);
        }

        public async Task<ResponseStreamHolder> CaptureExceptionsAsync(HttpMethod method, HttpContent content, int processId, ExceptionFormat format, CancellationToken token)
        {
            string uri = FormattableString.Invariant($"/exceptions?pid={processId}");
            var contentType = ContentTypeUtilities.MapFormatToContentType(format);
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

        public async Task<ResponseStreamHolder> CaptureStacksAsync(int processId, StackFormat format, CancellationToken token)
        {
            string uri = FormattableString.Invariant($"/stacks?pid={processId}");
            var contentType = ContentTypeUtilities.MapFormatToContentType(format);
            using HttpRequestMessage request = new(HttpMethod.Get, uri);
            request.Headers.Add(HeaderNames.Accept, contentType);

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

        public async Task<OperationResponse> CaptureParametersAsync(int processId, TimeSpan duration, string egressProvider, CaptureParametersConfiguration config, CapturedParameterFormat format, CancellationToken token)
        {
            string contentType = "";

            if (format == CapturedParameterFormat.JsonSequence)
            {
                contentType = ContentTypes.ApplicationJsonSequence;
            }
            else if (format == CapturedParameterFormat.NewlineDelimitedJson)
            {
                contentType = ContentTypes.ApplicationNDJson;
            }

            bool isInfinite = (duration == Timeout.InfiniteTimeSpan);
            string uri = FormattableString.Invariant($"/parameters?pid={processId}&durationSeconds={(isInfinite ? -1 : duration.TotalSeconds)}");
            if (!string.IsNullOrEmpty(egressProvider))
            {
                uri += FormattableString.Invariant($"&egressProvider={egressProvider}");
            }

            using HttpRequestMessage request = new(HttpMethod.Post, uri);

            request.Headers.Add(HeaderNames.Accept, contentType);

            string content = JsonSerializer.Serialize(config, DefaultJsonSerializeOptions);
            request.Content = new StringContent(content, Encoding.UTF8, ContentTypes.ApplicationJson);

            using HttpResponseMessage response = await SendAndLogAsync(request, HttpCompletionOption.ResponseContentRead, token).ConfigureAwait(false);

            switch (response.StatusCode)
            {
                case HttpStatusCode.Accepted:
                case HttpStatusCode.OK:
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

        public async Task<HttpResponseMessage> ApiCall(string routeAndQuery, CancellationToken token)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, routeAndQuery);
            HttpResponseMessage response = await SendAndLogAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            return response;
        }

        public async Task<ResponseStreamHolder> HttpEgressTraceAsync(int processId, int durationSeconds, CancellationToken token)
        {
            string uri = FormattableString.Invariant($"/trace?pid={processId}&durationSeconds={durationSeconds}");
            using HttpRequestMessage request = new(HttpMethod.Get, uri);

            using DisposableBox<HttpResponseMessage> responseBox = new(
                await SendAndLogAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    token).ConfigureAwait(false));

            switch (responseBox.Value.StatusCode)
            {
                case HttpStatusCode.OK:
                    return await ResponseStreamHolder.CreateAsync(responseBox).ConfigureAwait(false);
                case HttpStatusCode.BadRequest:
                case HttpStatusCode.TooManyRequests:
                    ValidateContentType(responseBox.Value, ContentTypes.ApplicationProblemJson);
                    throw await CreateValidationProblemDetailsExceptionAsync(responseBox.Value).ConfigureAwait(false);
                case HttpStatusCode.Unauthorized:
                    ThrowIfNotSuccess(responseBox.Value);
                    break;
            }

            throw await CreateUnexpectedStatusCodeExceptionAsync(responseBox.Value).ConfigureAwait(false);
        }

        public async Task<OperationResponse> EgressTraceAsync(int processId, int durationSeconds, string egressProvider, string tags, CancellationToken token)
        {
            string tagsQuery = string.IsNullOrEmpty(tags) ? string.Empty : $"&tags={tags}";
            string uri = FormattableString.Invariant($"/trace?pid={processId}&egressProvider={egressProvider}&durationSeconds={durationSeconds}{tagsQuery}");
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

        public async Task<List<OperationSummary>> GetOperations(string tags, CancellationToken token)
        {
            string tagsQuery = string.IsNullOrEmpty(tags) ? string.Empty : $"?tags={tags}";
            using HttpRequestMessage request = new(HttpMethod.Get, $"/operations{tagsQuery}");
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

        public async Task<HttpStatusCode> StopEgressOperation(Uri operation, CancellationToken token)
        {
            string operationUri = QueryHelpers.AddQueryString(operation.ToString(), "stop", "true");

            using HttpRequestMessage request = new(HttpMethod.Delete, operationUri);
            using HttpResponseMessage response = await SendAndLogAsync(request, HttpCompletionOption.ResponseContentRead, token).ConfigureAwait(false);

            switch (response.StatusCode)
            {
                case HttpStatusCode.Accepted:
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

        private static async Task<ValidationProblemDetails> ReadValidationProblemDetailsAsync(HttpResponseMessage responseMessage)
        {
            using Stream contentStream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<ValidationProblemDetails>(contentStream, ValidationProblemDetailsDeserializeOptions).ConfigureAwait(false);
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
                await ReadValidationProblemDetailsAsync(responseMessage),
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

        private static void ValidateContentType(HttpResponseMessage responseMessage, string expectedMediaType)
        {
            Assert.Equal(expectedMediaType, responseMessage.Content.Headers.ContentType?.MediaType);
        }

        private static string CreateLogsUriString(string processIdentifierQuery, TimeSpan duration, LogLevel? logLevel)
        {
            StringBuilder routeBuilder = new();
            routeBuilder.Append("/logs?");
            routeBuilder.Append(processIdentifierQuery);
            routeBuilder.Append('&');
            AppendDuration(routeBuilder, duration);
            if (logLevel.HasValue)
            {
                routeBuilder.Append("&level=");
                routeBuilder.Append(logLevel.Value.ToString("G"));
            }
            return routeBuilder.ToString();
        }

        private static string CreateTraceUriString(string processIdentifierQuery, TimeSpan duration, TraceProfile? profile = null)
        {
            StringBuilder routeBuilder = new();
            routeBuilder.Append("/trace?");
            routeBuilder.Append(processIdentifierQuery);
            routeBuilder.Append('&');
            AppendDuration(routeBuilder, duration);
            if (profile.HasValue)
            {
                routeBuilder.Append("&profile=");
                routeBuilder.Append(profile.Value.ToString("G"));
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

        private static JsonSerializerOptions CreateValidationProblemDetailsDeserializeOptions()
        {
            JsonSerializerOptions options = CreateJsonDeserializeOptions();
#if NET8_0_OR_GREATER
            // Workaround for https://github.com/dotnet/aspnetcore/issues/47223
            options.PropertyNameCaseInsensitive = true;
#endif
            return options;
        }
    }
}
