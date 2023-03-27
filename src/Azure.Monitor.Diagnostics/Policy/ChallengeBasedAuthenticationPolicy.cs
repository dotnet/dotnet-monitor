// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.Core.Pipeline;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading.Tasks;
using static Azure.Monitor.Diagnostics.Policy.AuthorizationChallengeParser;

namespace Azure.Monitor.Diagnostics.Policy;

/// <summary>
/// A <see cref="BearerTokenAuthenticationPolicy"/> pipeline policy that
/// automatically discovers the scope and authorization endpoint by issuing an
/// unauthenticated request and parsing the challenge response (WWW-Authenticate
/// header).
/// </summary>
internal sealed class ChallengeBasedAuthenticationPolicy : BearerTokenAuthenticationPolicy
{
    private const string DefaultScopeSuffix = "/.default";

    /// <summary>
    /// Challenges are cached using the authority as the key. This cache
    /// is shared by all pipelines using this policy to reduce the overall
    /// number of challenges.
    /// </summary>
    private static readonly ConcurrentDictionary<string, ChallengeParameters> s_challengeCache = new ConcurrentDictionary<string, ChallengeParameters>();

    /// <summary>
    /// Cached challenge parameters for this instance of the policy.
    /// </summary>
    private ChallengeParameters? _challenge;

    /// <summary>
    /// The default scope to use if the challenge response doesn't include one.
    /// </summary>
    private readonly string? _fallbackScope;

    /// <summary>
    /// Construct a new instance of the policy.
    /// </summary>
    /// <param name="tokenCredential">The token credential.</param>
    /// <param name="fallbackScope">Optional fallback scope to use if the challenge response does not specify a scope or audience.</param>
    public ChallengeBasedAuthenticationPolicy(TokenCredential tokenCredential, string? fallbackScope = null) : base(tokenCredential, Array.Empty<string>())
    {
        if (tokenCredential is null)
        {
            throw new ArgumentNullException(nameof(tokenCredential));
        }

        _fallbackScope = fallbackScope;
    }

    /// <inheritdoc cref="BearerTokenAuthenticationPolicy.AuthorizeRequestAsync(HttpMessage)" />
    protected override ValueTask AuthorizeRequestAsync(HttpMessage message)
    {
        if (TryGetChallengeParameters(message, out ChallengeParameters? challenge))
        {
            return AuthenticateAndAuthorizeRequestAsync(message, challenge!);
        }

        return default;
    }

    /// <inheritdoc cref="BearerTokenAuthenticationPolicy.AuthorizeRequest(HttpMessage)" />
    protected override void AuthorizeRequest(HttpMessage message)
    {
        if (TryGetChallengeParameters(message, out ChallengeParameters? challenge))
        {
            AuthenticateAndAuthorizeRequest(message, challenge!);
        }
    }

    /// <inheritdoc cref="BearerTokenAuthenticationPolicy.AuthorizeRequestOnChallengeAsync" />
    protected override async ValueTask<bool> AuthorizeRequestOnChallengeAsync(HttpMessage message)
    {
        if (TryCreateChallengeParameters(message, out ChallengeParameters challenge))
        {
            await AuthenticateAndAuthorizeRequestAsync(message, challenge).ConfigureAwait(false);
            return true;
        }

        return false;
    }

    /// <inheritdoc cref="BearerTokenAuthenticationPolicy.AuthorizeRequestOnChallenge" />
    protected override bool AuthorizeRequestOnChallenge(HttpMessage message)
    {
        if (TryCreateChallengeParameters(message, out ChallengeParameters challenge))
        {
            AuthenticateAndAuthorizeRequest(message, challenge);
            return true;
        }

        return false;
    }

    private ValueTask AuthenticateAndAuthorizeRequestAsync(HttpMessage message, ChallengeParameters challenge)
    {
        TokenRequestContext tokenRequestContext = CreateTokenRequestContextFromChallenge(challenge, message.Request.ClientRequestId);
        return AuthenticateAndAuthorizeRequestAsync(message, tokenRequestContext);
    }

    private void AuthenticateAndAuthorizeRequest(HttpMessage message, ChallengeParameters challenge)
    {
        TokenRequestContext tokenRequestContext = CreateTokenRequestContextFromChallenge(challenge, message.Request.ClientRequestId);
        AuthenticateAndAuthorizeRequest(message, tokenRequestContext);
    }

    /// <summary>
    /// Get the cached challenge parameters for use during authorization.
    /// </summary>
    /// <param name="message">The message that needs authorization.</param>
    /// <param name="challenge">The challenge parameters.</param>
    /// <returns>True if the challenge parameters are available.</returns>
    private bool TryGetChallengeParameters(HttpMessage message, out ChallengeParameters? challenge)
    {
        if ((challenge = _challenge) != null)
        {
            return true;
        }

        // Check the static cache.
        string authority = GetRequestAuthority(message.Request);
        if (s_challengeCache.TryGetValue(authority, out challenge))
        {
            _challenge = challenge;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Create challenge parameters by parsing the challenge response and
    /// extracting the scope and authorization URI.
    /// </summary>
    /// <param name="message">The message that resulted in a challenge (401).</param>
    /// <param name="challenge">The challenge parameters (scope and authorization URI)</param>
    /// <returns>True if the parameters could be parsed.</returns>
    /// <exception cref="UriFormatException">The authorization URI parsed from the WWW-Authenticate header was invalid.</exception>
    private bool TryCreateChallengeParameters(HttpMessage message, out ChallengeParameters challenge)
    {
        string authority = GetRequestAuthority(message.Request);
        string? scope = GetScope(message.Response);
        if (scope is null)
        {
            // Fallback. Check the static cache.
            if (s_challengeCache.TryGetValue(authority, out challenge))
            {
                _challenge = challenge;
                return true;
            }

            // Use the default scope, if set.
            scope = _fallbackScope;
            if (string.IsNullOrEmpty(scope))
            {
                return false;
            }
        }

        // We found a scope. Now find the authorization URI (OAuth endpoint)
        string? authorization
            = GetChallengeParameterFromResponse(message.Response, "Bearer", "authorization")
            ?? GetChallengeParameterFromResponse(message.Response, "Bearer", "authorization_uri");

        if (!Uri.TryCreate(authorization, UriKind.Absolute, out Uri authorizationUri))
        {
            throw new UriFormatException($"The challenge authorization URI '{authorization}' is invalid.");
        }

        _challenge = challenge = s_challengeCache[authority] = new ChallengeParameters(authorizationUri, new string[] { scope! });
        return true;
    }

    /// <summary>
    /// Parse the scope from the challenge header.
    /// </summary>
    /// <param name="response">The response containing the challenge header.</param>
    /// <returns>The scope or null if it couldn't be parsed.</returns>
    private static string? GetScope(Response response)
    {
        // Use the resource parameter in the Bearer scheme.
        string? scope = GetChallengeParameterFromResponse(response, "Bearer", "resource");
        if (scope != null)
        {
            // The resource is the "audience". Add the default suffix to make it a scope.
            return scope + DefaultScopeSuffix;
        }

        // Fall back to using the scope parameter, if available.
        return GetChallengeParameterFromResponse(response, "Bearer", "scope");
    }

    private static TokenRequestContext CreateTokenRequestContextFromChallenge(ChallengeParameters challenge, string parentRequestId)
        => new TokenRequestContext(challenge.Scopes, parentRequestId, tenantId: challenge.TenantId);

    private sealed class ChallengeParameters
    {
        public ChallengeParameters(Uri authorizationUri, string[] scopes)
        {
            TenantId = authorizationUri.Segments[1].Trim('/');
            Scopes = scopes;
        }

        /// <summary>
        /// Gets the "resource" or "scope" parameter from the challenge response. This should end with "/.default".
        /// </summary>
        public string[] Scopes { get; }

        /// <summary>
        /// Gets the tenant ID.
        /// </summary>
        public string TenantId { get; }
    }

    /// <summary>
    /// Gets the host name and port of the endpoint.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>The authority of the request.</returns>
    private static string GetRequestAuthority(Request request)
    {
        Uri uri = request.Uri.ToUri();

        string authority = uri.Authority;
        if (!authority.Contains(":") && uri.Port > 0)
        {
            // Append port for complete authority.
            authority = uri.Authority + ":" + uri.Port.ToString(CultureInfo.InvariantCulture);
        }

        return authority;
    }
}
