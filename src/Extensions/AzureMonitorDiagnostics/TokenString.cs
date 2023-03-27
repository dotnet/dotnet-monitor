// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.AzureMonitorDiagnostics;

/// <summary>
/// Holds a set of tokens parsed from a semi-colon separated list of "key=value" tokens.
/// </summary>
public class TokenString
{
    private readonly Token[] _tokens;

    /// <summary>
    /// The constructor is private. Use <see cref="TryParse(string, out TokenString)"/>.
    /// </summary>
    /// <param name="tokens">Array of tokens.</param>
    private TokenString(Token[] tokens)
    {
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
    }

    /// <summary>
    /// Look-up a value by key.
    /// The look-up is case insensitive.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The value.</returns>
    /// <exception cref="KeyNotFoundException">The key was not found.</exception>
    public string this[string key] => GetValue(key);

    /// <summary>
    /// Look-up a value by key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="stringComparison">Optional string comparison method.</param>
    /// <returns>The value.</returns>
    /// <exception cref="KeyNotFoundException">The key was not found.</exception>
    public string GetValue(string key, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
    {
        return TryGetValue(key, out string? value, stringComparison) ? value : throw new KeyNotFoundException();
    }

    /// <summary>
    /// Try tp look up a value by key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The found value.</param>
    /// <param name="stringComparison">Optional string comparison method.</param>
    /// <returns>True if the key was found and <paramref name="value"/> contains the value. False otherwise.</returns>
    public bool TryGetValue(string key, [NotNullWhen(true)] out string? value, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        => TryGetValue(_tokens, key, out value, stringComparison);

    /// <summary>
    /// Try to parse the given string into an <see cref="TokenString"/>.
    /// The string must consist of key/value pairs separated by semicolons.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <returns>True if <paramref name="value"/> was successfully parsed.</returns>
    public static bool TryParse(string? value, [NotNullWhen(true)] out TokenString? tokenString)
    {
        // Null is invalid.
        if (value is null)
        {
            tokenString = null;
            return false;
        }

        List<Token> tokens = new();
        foreach ((int, int) split in Tokenize(value))
        {
            (int start, int length) = split;

            // Trim whitespace from start
            while (length > 0 && char.IsWhiteSpace(value[start]))
            {
                start++;
                length--;
            }

            // Ignore (allow) empty tokens.
            if (length == 0)
            {
                continue;
            }

            // Find key-value separator.
            int indexOfEquals = value.IndexOf('=', start, length);
            if (indexOfEquals < 0)
            {
                tokenString = null;
                return false;
            }

            // Extract key
            int keyLength = indexOfEquals - start;
            string key = value.Substring(start, keyLength).TrimEnd();
            if (key.Length == 0)
            {
                // Key is blank
                tokenString = null;
                return false;
            }

            // Check for duplicate keys
            if (TryGetValue(tokens, key, out _))
            {
                tokenString = null;
                return false;
            }

            // Add token
            tokens.Add(new Token(
                key,
                value.Substring(indexOfEquals + 1, length - keyLength - 1).Trim()
                ));
        }

        tokenString = new TokenString(tokens.ToArray());
        return true;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        StringBuilder sb = new();
        foreach (Token token in _tokens)
        {
            if (sb.Length > 0)
            {
                sb.Append(';');
            }

            sb.Append(token.Key).Append('=').Append(token.Value);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Split the given string into tokens.
    /// </summary>
    /// <param name="value">The string to tokenize.</param>
    /// <param name="separator">The separator character.</param>
    /// <returns>An iterator over the ranges representing extents of the tokens in <paramref name="value"/>.</returns>
    private static IEnumerable<(int start, int length)> Tokenize(string value, char separator = ';')
    {
        for (int start = 0, end; start < value.Length; start = end + 1)
        {
            end = value.IndexOf(separator, start);
            if (end < 0)
            {
                end = value.Length;
            }

            yield return (start, end - start);
        }
    }

    /// <summary>
    /// Try to retrieve the value of a token with the given key from the
    /// given list of tokens.
    /// </summary>
    /// <param name="tokens">The collection of tokens.</param>
    /// <param name="key">The key of the token you're looking for.</param>
    /// <param name="value">On success, the value of the token.</param>
    /// <param name="stringComparison">String comparison method.</param>
    /// <returns>True if the token was found.</returns>
    private static bool TryGetValue(IReadOnlyList<Token> tokens, string key, [NotNullWhen(true)] out string? value, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
    {
        foreach (Token token in tokens)
        {
            if (string.Equals(key, token.Key, stringComparison))
            {
                value = token.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// A key/value pair.
    /// </summary>
    private readonly struct Token
    {
        public Token(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public readonly string Key;
        public readonly string Value;
    }
}
