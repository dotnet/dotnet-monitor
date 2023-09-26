// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal interface IExtension
    {
        /// <summary>
        /// Gets a friendly name to describe this instance of an <see cref="IExtension"/>. This is used in Logs.
        /// </summary>
        string DisplayName { get; }
    }
}
