// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.FileProviders;
using System.IO;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    public static class ExtensionRepositoryUtilities
    {
        public static bool ExtensionDefinitionExists(IFileProvider fileSystem, string extensionPath)
        {
            // Just checks - is there an extensions.json file here, regardless of what's inside it?
            IDirectoryContents extensionDir = fileSystem.GetDirectoryContents(extensionPath);

            if (extensionDir.Exists)
            {
                IFileInfo defFile = fileSystem.GetFileInfo(Path.Combine(extensionPath, Constants.ExtensionDefinitionFile));
                if (defFile.Exists && !defFile.IsDirectory)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
