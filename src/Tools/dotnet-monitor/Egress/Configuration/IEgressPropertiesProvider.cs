// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration
{
    /// <summary>
    /// Provides strongly-typed access to the values described in the Egress:Properties section.
    /// </summary>
    internal interface IEgressPropertiesProvider
    {
        /// <summary>
        /// Gets the set of keys defined as a <see cref="IDictionary{string, string}"/> with values populated.
        /// </summary>
        /// <returns><see cref="IDictionary{string, string}"/> representing the set of properties.</returns>
        IDictionary<string, string> GetAllProperties();

        /// <summary>
        /// Attempts to get the value associated with the specified key from the Egress:Properties section.
        /// </summary>
        bool TryGetPropertyValue(string key, out string value);
    }
}
