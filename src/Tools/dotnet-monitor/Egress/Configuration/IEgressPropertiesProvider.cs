// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration
{
    /// <summary>
    /// Provides strongly-typed access to the values described in the Egress:Properties section.
    /// </summary>
    internal interface IEgressPropertiesProvider
    {
        /// <summary>
        /// Attempts to get the value associated with the specified key from the Egress:Properties section.
        /// </summary>
        bool TryGetPropertyValue(string key, out string value);
    }
}
