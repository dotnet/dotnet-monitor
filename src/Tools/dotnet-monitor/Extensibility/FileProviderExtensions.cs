// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.FileProviders;
using System.IO;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    public static class FileProviderExtensions
    {
        public static bool TryGetExtensionDefinitionPath(this IFileProvider fileProvider, string extensionSubPath, out string definitionPath)
        {
            definitionPath = Path.Combine(extensionSubPath, Constants.ExtensionDefinitionFile);
            IFileInfo defFile = fileProvider.GetFileInfo(definitionPath);
            if (defFile.Exists && !defFile.IsDirectory)
            {
                return true;
            }

            return false;
        }
    }
}
