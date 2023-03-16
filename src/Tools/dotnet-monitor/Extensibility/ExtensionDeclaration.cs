// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal class ExtensionDeclaration
    {
        /// <summary>
        /// Id of the extension. This should be namespace qualified like nuget packages.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The version of the extension. This should be SemVer 2.0.
        /// </summary>
        public Version Version { get; set; }

        /// <summary>
        /// This is the relative path to the executable file to be launched.
        /// </summary>
        public string Program { get; set; }

        /// <summary>
        /// This is the name that users specify in configuration to refer to the extension.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// An array of strings declaring what types of extensions are supported by this extension.
        /// This should contain values from <see cref="ExtensionTypes"/>.
        /// </summary>
        public string[] SupportedExtensionTypes { get; set; }

        /// <summary>
        /// Instructs dotnet-monitor to launch the extension using the shared .NET host (e.g. dotnet.exe).
        /// </summary>
        public bool UseSharedDotNetHost { get; set; }
    }
}
