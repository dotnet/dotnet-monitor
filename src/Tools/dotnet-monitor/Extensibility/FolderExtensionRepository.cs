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
            // Need to enumerate all directories inside current directory of _fileSystem, then send each to ExtensionFileExists

            IDirectoryContents extensionsDirs = _fileSystem.GetDirectoryContents(""); // This holds all the possible extension dirs

            ILogger<ProgramExtension> logger = _loggerFactory.CreateLogger<ProgramExtension>();

            foreach (var extensionDir in extensionsDirs)
            {
                var extensionDirName = extensionDir.Name;

                if (ExtensionRepositoryUtilities.ExtensionDefinitionExists(_fileSystem, extensionDirName))
                {
                    var tempExtension = new ProgramExtension(extensionName, _targetFolder, _fileSystem, Path.Combine(extensionDirName, Constants.ExtensionDefinitionFile), logger);
                    if (extensionName == tempExtension.ExtensionDeclaration.Value.DisplayName)
                    {
                        extension = tempExtension;
                        return true;
                    }
                }
            }

            extension = null;
            return false;
        }
    }
}
