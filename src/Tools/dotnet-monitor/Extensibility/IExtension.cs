// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal interface IExtension
    {
        /// <summary>
        /// Gets a friendly name to describe this instance of an <see cref="IExtension"/>. This is used in Logs.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Queries the underlying Extension to determine if the given <typeparamref name="TExtensionType"/> is supported by this instance.
        /// </summary>
        /// <typeparam name="TExtensionType">The desired extension type to be run.</typeparam>
        /// <param name="extension">The typed extension returned as an out parameter.</param>
        /// <returns>A <see cref="bool"/> <see langword="true"/> if the given type is supported and passed in the out param; <see cref="bool"/> <see langword="false"/> otherwise.</returns>
        bool TryGetTypedExtension<TExtensionType>(out TExtensionType extension) where TExtensionType : class, IExtension;
    }
}
