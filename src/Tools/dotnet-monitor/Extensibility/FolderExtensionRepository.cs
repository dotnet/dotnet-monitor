// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal class FolderExtensionRepository : ExtensionRepository
    {
        private readonly string _targetFolder;
        private readonly IFileProvider _fileSystem;
        private readonly ILogger<ProgramExtension> _logger;

        public FolderExtensionRepository(IFileProvider fileSystem, ILogger<ProgramExtension> logger, string targetFolder)
        {
            _fileSystem = fileSystem;

            _targetFolder = targetFolder;

            _logger = logger;
        }

        public override bool TryFindExtension(string extensionName, out IExtension extension)
        {
            IDirectoryContents extensionDirs = _fileSystem.GetDirectoryContents(string.Empty);

            foreach (IFileInfo extensionDir in extensionDirs)
            {
                if (_fileSystem.TryGetExtensionDefinitionPath(extensionDir.Name, out string definitionPath))
                {
                    var currExtension = new ProgramExtension(extensionName, _targetFolder, _fileSystem, definitionPath, _logger);
                    if (extensionName == currExtension.Declaration.Name)
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
