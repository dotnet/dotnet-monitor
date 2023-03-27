// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;

namespace Azure.Monitor.Diagnostics;

/// <summary>
/// Extension methods for building <see cref="HttpMessage"/> requests.
/// </summary>
internal static class HttpMessageExtensions
{
    /// <summary>
    /// Append a query parameter and value to the request.
    /// </summary>
    /// <param name="message">The message with the request to modify.</param>
    /// <param name="name">The query parameter name.</param>
    /// <param name="value">The query parameter value.</param>
    /// <param name="escapeValue">Whether to escape the value.</param>
    /// <returns>The modified message for fluent chaining.</returns>
    public static HttpMessage WithQuery(this HttpMessage message, string name, string value, bool escapeValue = false)
    {
        message.Request.Uri.AppendQuery(name, value, escapeValue);
        return message;
    }

    /// <summary>
    /// Add a header value to the request.
    /// </summary>
    /// <param name="message">The message with the request to modify.</param>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value.</param>
    /// <returns>The modified message for fluent chaining.</returns>
    public static HttpMessage WithHeader(this HttpMessage message, string name, string value)
    {
        message.Request.Headers.Add(name, value);
        return message;
    }
}
