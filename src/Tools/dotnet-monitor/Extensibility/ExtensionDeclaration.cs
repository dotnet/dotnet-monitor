// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal class ExtensionDeclaration
    {
        /// <summary>
        /// This is the name that users specify in configuration to refer to the extension.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// If specified, the executable file (without extension) to be launched when executing the extension.
        /// </summary>
        /// <remarks>
        /// Either <see cref="ExecutableFileName"/> or <see cref="AssemblyFileName"/> must be specified.
        /// </remarks>
        public string ExecutableFileName { get; set; }

        /// <summary>
        /// If specified, executes the extension using the shared .NET host (e.g. dotnet.exe) with the specified entry point assembly (without extension). 
        /// </summary>
        /// <remarks>
        /// Either <see cref="ExecutableFileName"/> or <see cref="AssemblyFileName"/> must be specified.
        /// </remarks>
        public string AssemblyFileName { get; set; }

        public void Validate()
        {
            bool hasAssemblyFileName = string.IsNullOrEmpty(AssemblyFileName);
            bool hasExecutableFileName = string.IsNullOrEmpty(ExecutableFileName);

            if (hasAssemblyFileName && hasExecutableFileName)
            {
                ExtensionException.ThrowInvalidManifest(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Strings.ErrorMessage_TwoFieldsCannotBeSpecified,
                        nameof(AssemblyFileName),
                        nameof(ExecutableFileName)));
            }

            if (!hasAssemblyFileName && !hasExecutableFileName)
            {
                ExtensionException.ThrowInvalidManifest(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Strings.ErrorMessage_TwoFieldsMissing,
                        nameof(AssemblyFileName),
                        nameof(ExecutableFileName)));
            }
        }
    }
}
