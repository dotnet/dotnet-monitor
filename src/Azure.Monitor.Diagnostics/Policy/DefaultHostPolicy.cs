// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.Core.Pipeline;
using System;

namespace Azure.Monitor.Diagnostics.Policy;

/// <summary>
/// An <see cref="HttpPipelinePolicy"/> that sets the default host for all
/// requests.
/// </summary>
internal sealed class DefaultHostPolicy : HttpPipelineSynchronousPolicy
{
    private Uri _host;

    /// <summary>
    /// The endpoint for all requests.
    /// </summary>
    public Uri Host
    {
        get => _host;
        set => _host = ValidateHost(value, nameof(Host));
    }

    /// <summary>
    /// Construct a new <see cref="DefaultHostPolicy"/> instance.
    /// </summary>
    /// <param name="host">The default host to use.</param>
    /// <exception cref="ArgumentNullException"><paramref name="host"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="host"/> is not an absolute URI.</exception>
    public DefaultHostPolicy(Uri host)
    {
        _host = ValidateHost(host, nameof(host));
    }

    /// <summary>
    /// Update the request to include the default host if
    /// it's not already set.
    /// </summary>
    /// <param name="message">The request message.</param>
    public override void OnSendingRequest(HttpMessage message)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        RequestUriBuilder uriBuilder = message.Request.Uri;
        if (uriBuilder.Host is null)
        {
            uriBuilder.Reset(new Uri(Host, uriBuilder.PathAndQuery));
        }
    }

    private static Uri ValidateHost(Uri host, string paramName)
    {
        if (host is null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (!host.IsAbsoluteUri)
        {
            throw new ArgumentException("The host endpoint must be an absolute URI", paramName);
        }

        return host;
    }
}
