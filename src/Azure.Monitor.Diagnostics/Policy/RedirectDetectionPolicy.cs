// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.Core.Pipeline;
using System;
using System.Net;

namespace Azure.Monitor.Diagnostics.Policy;

/// <summary>
/// An <see cref="HttpPipelinePolicy"/> that detect when redirection happens.
/// This is used to avoid repeatedly hitting redirections from a global
/// endpoint to a regional one.
/// </summary>
internal sealed class RedirectDetectionPolicy : HttpPipelineSynchronousPolicy
{
    /// <summary>
    /// Action to invoke when redirection is detected.
    /// </summary>
    private readonly Action<Uri> _onRedirect;

    private const string LocationHeaderName = "Location";

    /// <summary>
    /// Construct a new <see cref="RedirectDetectionPolicy"/> instance.
    /// </summary>
    /// <param name="onRedirect">The action to call when redirection is detected.</param>
    /// <exception cref="ArgumentNullException"><paramref name="onRedirect"/> is null.</exception>
    public RedirectDetectionPolicy(Action<Uri> onRedirect)
        => _onRedirect = onRedirect ?? throw new ArgumentNullException(nameof(onRedirect));

    /// <summary>
    /// Handle the response and detect when a new redirection happens.
    /// </summary>
    /// <param name="message">The request message.</param>
    public override void OnReceivedResponse(HttpMessage message)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (IsRedirection(message.Response.Status)
            && TryGetRedirectLocation(message, out Uri? redirectedEndpoint))
        {
            _onRedirect(redirectedEndpoint!);
        }
    }

    /// <summary>
    /// Check whether the given status code is redirect.
    /// </summary>
    /// <param name="status">The status code to check.</param>
    /// <returns>True, if redirection, otherwise, false.</returns>
    private static bool IsRedirection(int status)
    {
        switch (status)
        {
            case (int)HttpStatusCode.MultipleChoices:
            case (int)HttpStatusCode.MovedPermanently:
            case (int)HttpStatusCode.Found:
            case (int)HttpStatusCode.RedirectMethod:
            case (int)HttpStatusCode.TemporaryRedirect:
            case 308:   // Permanent redirect
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Get the redirect endpoint from the given <paramref name="message"/>.
    /// </summary>
    /// <param name="message">The request message.</param>
    /// <param name="location">The redirect endpoint, if found.</param>
    /// <returns>True, if redirect endpoint was found, otherwise, false.</returns>
    private static bool TryGetRedirectLocation(HttpMessage message, out Uri? location)
    {
        location = null;
        return message.Response.Headers.TryGetValue(LocationHeaderName, out string? locationString)
            && Uri.TryCreate(locationString, UriKind.Absolute, out location);
    }
}
