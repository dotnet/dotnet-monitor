// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal class ToolsExtensionRepository : ExtensionRepository
    {
        private readonly string _targetFolder;
        private readonly IFileProvider _fileSystem;
        private readonly ILoggerFactory _loggerFactory;

        public ToolsExtensionRepository(IFileProvider fileSystem, ILoggerFactory loggerFactory, int resolvePriority, string targetFolder)
            : base(resolvePriority, string.Format(CultureInfo.CurrentCulture, Strings.Message_FolderExtensionRepoName, targetFolder))
        {
            _fileSystem = fileSystem;

            _targetFolder = targetFolder;

            _loggerFactory = loggerFactory;
        }

        public override bool TryFindExtension(string extensionName, out IExtension extension)
        {
            const string storeDirectory = ".store";
            IDirectoryContents toolsStoreDir = _fileSystem.GetDirectoryContents(storeDirectory);
            ILogger<ProgramExtension> logger = _loggerFactory.CreateLogger<ProgramExtension>();

            foreach (IFileInfo tool in toolsStoreDir)
            {
                string extensionVer = _fileSystem.GetDirectoryContents(Path.Combine(storeDirectory, tool.Name)).First().Name;

                string netVer = "net7.0"; // TODO: Still need to determine this

                string extensionPath = Path.Combine(storeDirectory, tool.Name, extensionVer, tool.Name, extensionVer, "tools", netVer, "any");

                if (ExtensionRepositoryUtilities.ExtensionDefinitionExists(_fileSystem, extensionPath))
                {
                    var currExtension = new ProgramExtension(extensionName, _targetFolder, _fileSystem, Path.Combine(extensionPath, Constants.ExtensionDefinitionFile), extensionName, logger);
                    if (extensionName == currExtension.ExtensionDeclaration.Value.DisplayName)
                    {
                        extension = currExtension;
                        return true;
                    }
                }
            }

            extension = null;
            return false;
        }
    }
}
