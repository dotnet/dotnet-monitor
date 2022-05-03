// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    /// <summary>
    /// Interface for service used to lookup extensions based on a moniker.
    /// </summary>
    internal interface IExtensionDiscoverer
    {
        /// <summary>
        /// Gets a callable <see cref="IExtension"/> for the given extension moniker
        /// </summary>
        /// <param name="extensionMoniker"><see cref="string"/> moniker used to lookup the specified extension.</param>
        /// <returns>A <see cref="IExtension"/> that can be executed.</returns>
        TExtensionType? FindExtension<TExtensionType>(string extensionMoniker) where TExtensionType : class, IExtension;
    }
}
