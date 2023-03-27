// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.AzureMonitorDiagnostics;

/// <summary>
/// Represents a connection string for accessing Azure Monitor
/// diagnostic services.
/// </summary>
internal sealed class ConnectionString
{
    private readonly TokenString _tokenString;

    /// <summary>
    /// Construct a new <see cref="ConnectionString"/> instance from
    /// a set of parsed key/value pairs (tokens).
    /// </summary>
    /// <param name="tokenString">The parsed key-value pairs.</param>
    /// <exception cref="ArgumentNullException"><paramref name="tokenString"/> is null.</exception>
    public ConnectionString(TokenString tokenString)
    {
        _tokenString = tokenString ?? throw new ArgumentNullException(nameof(tokenString));
    }

    /// <summary>
    /// Get the instrumentation key. This key will always be present.
    /// </summary>
    public string InstrumentationKey => _tokenString[nameof(InstrumentationKey)];

    /// <summary>
    /// Get the endpoint for the given feature of Azure Monitor.
    /// </summary>
    /// <param name="feature">The feature name.</param>
    /// <returns>The endpoint for the requested feature or null.</returns>
    public Uri? GetFeatureEndpoint(string feature)
    {
        if (string.IsNullOrEmpty(feature))
        {
            throw new ArgumentException($"'{nameof(feature)}' cannot be null or empty.", nameof(feature));
        }

        if (_tokenString.TryGetValue(feature + "Endpoint", out string? endpoint))
        {
            return new Uri(endpoint);
        }

        if (_tokenString.TryGetValue("EndpointSuffix", out string? endpointSuffix))
        {
            string featurePrefix = ResolveFeaturePrefix(feature);
            return new Uri("https://" + featurePrefix.ToLowerInvariant() + "." + endpointSuffix, UriKind.Absolute);
        }

        return null;
    }

    /// <summary>
    /// IngestionEndpoint is a special case feature and the prefix is dc instead of just feature.
    /// </summary>
    /// <param name="feature">feature name</param>
    /// <returns>The resolved feature prefix.</returns>
    private static string ResolveFeaturePrefix(string feature) => string.Equals(feature, "ingestion", StringComparison.OrdinalIgnoreCase) ? "dc" : feature;

    /// <summary>
    /// Create a new <see cref="ConnectionString"/> from an unadorned instrumentation key.
    /// The instrumentation key identifies Application Insights resource in Azure Public
    /// Cloud using the global ingestion endpoints.
    /// </summary>
    /// <param name="instrumentationKey">The instrumentation key.</param>
    /// <returns>A connection string.</returns>
    /// <exception cref="ArgumentException"><paramref name="instrumentationKey"/> is invalid.</exception>
    public static ConnectionString FromInstrumentationKey(string instrumentationKey)
    {
        if (string.IsNullOrEmpty(instrumentationKey))
        {
            throw new ArgumentException($"'{nameof(instrumentationKey)}' cannot be null or empty.", nameof(instrumentationKey));
        }

        string connectionString = nameof(InstrumentationKey) + "=" + instrumentationKey;
        return TokenString.TryParse(connectionString, out TokenString? tokenString)
            ? new ConnectionString(tokenString)
            : throw new ArgumentException("Instrumentation key contains invalid characters.", nameof(instrumentationKey));
    }
}
