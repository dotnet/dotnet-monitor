// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.Monitor.Diagnostics.Policy;
using System;
using System.ComponentModel;

namespace Azure.Monitor.Diagnostics;

/// <summary>
/// Extension methods for <see cref="ClientOptions"/>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class ClientOptionsExtensions
{
    /// <summary>
    /// Set the base endpoint in the given <paramref name="options"/> to all requests.
    /// If redirection happens in any request of the pipeline, the new endpoint will be cached and used
    /// for all future requests.
    /// </summary>
    /// <param name="options">Options to update.</param>
    /// <param name="baseEndpoint">The base endpoint to use for all requests.</param>
    /// <returns><paramref name="options"/> for fluent chaining.</returns>
    public static ClientOptions SetEndpointRedirectionCachePolicy(this ClientOptions options, Uri baseEndpoint)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var defaultHostPolicy = new DefaultHostPolicy(baseEndpoint);
        options.AddPolicy(defaultHostPolicy, HttpPipelinePosition.PerCall);

        var redirectDetectionPolicy = new RedirectDetectionPolicy(
            redirect => defaultHostPolicy.Host = new Uri(redirect, "/")
            );
        options.AddPolicy(redirectDetectionPolicy, HttpPipelinePosition.PerRetry);
        return options;
    }

    /// <summary>
    /// Add an authorization policy that automatically determines the scope.
    /// </summary>
    /// <param name="options">Options to update.</param>
    /// <param name="tokenCredential">The token credential to be used to get an access token.</param>
    /// <param name="fallbackScope">Optional scope to use if the challenge response doesn't include one.</param>
    /// <returns><paramref name="options"/> for fluent chaining.</returns>
    /// <remarks>If <paramref name="tokenCredential"/> is null, then the authorization policy is not added.</remarks>
    public static ClientOptions SetChallengeBasedAuthenticationPolicy(this ClientOptions options, TokenCredential? tokenCredential, string fallbackScope)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (tokenCredential != null)
        {
            // This policy needs to be PerRetry because authorization headers are removed on redirects.
            var policy = new ChallengeBasedAuthenticationPolicy(tokenCredential, fallbackScope);
            options.AddPolicy(policy, HttpPipelinePosition.PerRetry);
        }

        return options;
    }

    /// <summary>
    /// Add api-version to all requests.
    /// </summary>
    /// <param name="options">Options to update.</param>
    /// <param name="apiVersion"></param>
    /// <param name="pipelinePosition">The pipeline position of this policy.</param>
    /// <returns><paramref name="options"/> for fluent chaining.</returns>
    public static ClientOptions SetApiVersionPolicy(this ClientOptions options, string apiVersion, HttpPipelinePosition pipelinePosition = HttpPipelinePosition.PerCall)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var policy = new ApiVersionPolicy(apiVersion);
        options.AddPolicy(policy, pipelinePosition);
        return options;
    }
}
