// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal class FolderExtensionRepository : ExtensionRepository
    {
        private readonly string _targetFolder;
        private readonly IFileProvider _fileSystem;
        private readonly ILoggerFactory _loggerFactory;

        public FolderExtensionRepository(IFileProvider fileSystem, ILoggerFactory loggerFactory, int resolvePriority, string targetFolder)
            : base(resolvePriority, string.Format(CultureInfo.CurrentCulture, Strings.Message_FolderExtensionRepoName, targetFolder))
        {
            _fileSystem = fileSystem;

            _targetFolder = targetFolder;

            _loggerFactory = loggerFactory;
        }

        public override bool TryFindExtension(string extensionName, out IExtension extension)
        {
            IDirectoryContents extensionDirs = _fileSystem.GetDirectoryContents(string.Empty);
            ILogger<ProgramExtension> logger = _loggerFactory.CreateLogger<ProgramExtension>();

            foreach (IFileInfo extensionDir in extensionDirs)
            {
                if (_fileSystem.TryGetExtensionDefinitionPath(extensionDir.Name, out string definitionPath))
                {
                    var currExtension = new ProgramExtension(extensionName, _targetFolder, _fileSystem, definitionPath, logger);
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
