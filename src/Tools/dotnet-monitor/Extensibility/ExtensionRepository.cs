// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal abstract class ExtensionRepository
    {
        private readonly int _resolvePriority;
        private readonly string _displayName;

        public ExtensionRepository(int resolvePriority, string displayName)
        {
            _resolvePriority = resolvePriority;
            _displayName = displayName;
        }

        /// <summary>
        /// Gets the integer that describes the order used to probe multiple repositories
        /// </summary>
        public int ResolvePriority => _resolvePriority;

        /// <summary>
        /// Gets a friendly name to describe this instance of an <see cref="IExtensionRepository"/>. This is used in Logs.
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// Gets a callable <see cref="IExtension"/> for the given extension moniker.
        /// </summary>
        /// <param name="extensionMoniker"><see cref="string"/> moniker used to lookup the specified extension.</param>
        /// <param name="extension">The discovered <see cref="IExtension"/> is returned via this out parameter.</param>
        /// <returns>A <see cref="bool"/> <see langword="true"/> if the desired extension was found; <see langword="false"/> otherwise.</returns>
        public abstract bool TryFindExtension(string extensionMoniker, out IExtension extension);
    }
}
