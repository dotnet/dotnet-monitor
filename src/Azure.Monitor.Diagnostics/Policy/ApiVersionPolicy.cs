// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.Core.Pipeline;
using System;

namespace Azure.Monitor.Diagnostics.Policy;

/// <summary>
/// A <see cref="HttpPipelinePolicy"/> that appends the api-version to
/// the query parameters of every request.
/// </summary>
internal sealed class ApiVersionPolicy : HttpPipelineSynchronousPolicy
{
    private readonly string _apiVersion;

    /// <summary>
    /// Construct a new <see cref="ApiVersionPolicy"/> instance.
    /// </summary>
    /// <param name="apiVersion">The api version string.</param>
    public ApiVersionPolicy(string apiVersion)
    {
        if (string.IsNullOrEmpty(apiVersion))
        {
            throw new ArgumentException($"'{nameof(apiVersion)}' cannot be null or empty.", nameof(apiVersion));
        }

        _apiVersion = apiVersion;
    }

    /// <summary>
    /// Append the api-version query parameter if it is not already set.
    /// </summary>
    /// <param name="message">The request message.</param>
    public override void OnSendingRequest(HttpMessage message)
    {
        RequestUriBuilder uri = message.Request.Uri;
        if (!uri.Query.Contains("api-version="))
        {
            uri.AppendQuery("api-version", _apiVersion, escapeValue: false);
        }

        base.OnSendingRequest(message);
    }
}
