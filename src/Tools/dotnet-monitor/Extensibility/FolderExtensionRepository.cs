// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal class FolderExtensionRepository : ExtensionRepository
    {
        private const string ExtensionDefinitionFile = "extension.json";
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
            IFileInfo extensionDir = _fileSystem.GetFileInfo(extensionName);

            if (extensionDir.Exists && extensionDir.IsDirectory)
            {
                IFileInfo defFile = _fileSystem.GetFileInfo(Path.Combine(extensionName, ExtensionDefinitionFile));
                if (defFile.Exists && !defFile.IsDirectory)
                {
                    ILogger<ProgramExtension> logger = _loggerFactory.CreateLogger<ProgramExtension>();
                    extension = new ProgramExtension(extensionName, _targetFolder, _fileSystem, Path.Combine(extensionName, ExtensionDefinitionFile), logger);
                    return true;
                }
            }

            extension = null;
            return false;
        }
    }
}
