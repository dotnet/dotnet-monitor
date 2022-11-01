// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal class ToolsExtensionRepository : FolderExtensionRepository
    {
        public ToolsExtensionRepository(IFileProvider fileSystem, ILoggerFactory loggerFactory, int resolvePriority, string targetFolder)
            : base(fileSystem, loggerFactory, resolvePriority, targetFolder)
        {
        }

        public override bool TryFindExtension(string extensionName, out IExtension extension)
        {
            string extensionVer = _fileSystem.GetDirectoryContents(Path.Combine(".store", extensionName)).First().Name;

            string netVer = "net7.0"; // TODO: Still need to determine this

            string extensionPath = Path.Combine(".store", extensionName, extensionVer, extensionName, extensionVer, "tools", netVer, "any");

            if (ExtensionDefinitionExists(extensionPath))
            {
                ILogger<ProgramExtension> logger = _loggerFactory.CreateLogger<ProgramExtension>();
                extension = new ProgramExtension(extensionName, _targetFolder, _fileSystem, Path.Combine(extensionPath, ExtensionDefinitionFile), extensionName, logger);
                return true;
            }

            extension = null;
            return false;
        }
    }
}
