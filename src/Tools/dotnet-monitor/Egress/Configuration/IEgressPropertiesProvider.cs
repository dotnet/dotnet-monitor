// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration
{
    /// <summary>
    /// Provides strongly-typed access to the values described in the Egress:Properties section.
    /// </summary>
    internal interface IEgressPropertiesProvider
    {
        /// <summary>
        /// Gets the set of keys defined.
        /// </summary>
        /// <returns><see cref="IEnumerable{string}"/> that has the set of keys that can be retreived.</returns>
        IEnumerable<string> GetKeys();

        /// <summary>
        /// Attempts to get the value associated with the specified key from the Egress:Properties section.
        /// </summary>
        bool TryGetPropertyValue(string key, out string value);
    }
}
