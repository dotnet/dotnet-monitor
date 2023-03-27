// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using System;

namespace Azure.Monitor.Diagnostics;

/// <summary>
/// <see cref="Core.Pipeline.HttpPipeline"/> options for <see cref="DiagnosticsClient"/>.
/// Adds the service endpoint to all requests and handles redirection.
/// Adds the api-version query parameter on all requests.
/// Adds an authorization handler, if necessary.
/// </summary>
internal sealed class DiagnosticsClientPipelineOptions : ClientOptions
{
    /// <summary>
    /// The scope for Azure Monitor in public cloud.
    /// </summary>
    private const string FallbackScope = "https://monitor.azure.com//.default";

    /// <summary>
    /// Construct a new <see cref="IngestionClientPipelineOptions"/> instance.
    /// </summary>
    /// <param name="options">Ingestion client options.</param>
    public DiagnosticsClientPipelineOptions(DiagnosticsClientOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (!options.Endpoint.IsAbsoluteUri)
        {
            throw new ArgumentException("The endpoint must be an absolute URI.", nameof(options));
        }

        _ = this.SetEndpointRedirectionCachePolicy(options.Endpoint)
                .SetApiVersionPolicy(options.ApiVersion)
                .SetChallengeBasedAuthenticationPolicy(options.TokenCredential, FallbackScope);
    }
}
