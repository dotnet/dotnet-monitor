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
        private const string ExtensionDefinitionFile = "extension.jsonc";
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

        public override bool TryFindExtension(string extensionMoniker, out IExtension extension)
        {
            IFileInfo extensionDir = _fileSystem.GetFileInfo(extensionMoniker);

            if (extensionDir.Exists && extensionDir.IsDirectory)
            {
                IFileInfo defFile = _fileSystem.GetFileInfo(Path.Combine(extensionMoniker, ExtensionDefinitionFile));
                if (defFile.Exists && !defFile.IsDirectory)
                {
                    ILogger<ProgramExtension> logger = _loggerFactory.CreateLogger<ProgramExtension>();
                    extension = new ProgramExtension(Path.Combine(_targetFolder, extensionMoniker, ExtensionDefinitionFile), logger);
                    return true;
                }
            }

            extension = null;
            return false;
        }
    }
}
