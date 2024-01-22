// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal abstract class ExtensionRepository
    {
        /// <summary>
        /// Gets a callable <see cref="IExtension"/> for the given extension moniker.
        /// </summary>
        /// <param name="extensionName"><see cref="string"/> moniker used to lookup the specified extension.</param>
        /// <param name="extension">The discovered <see cref="IExtension"/> is returned via this out parameter.</param>
        /// <returns>A <see cref="bool"/> <see langword="true"/> if the desired extension was found; <see langword="false"/> otherwise.</returns>
        public abstract bool TryFindExtension(string extensionName, out IExtension extension);
    }
}
