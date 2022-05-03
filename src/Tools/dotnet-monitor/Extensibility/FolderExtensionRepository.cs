// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System;
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

            // This is the root that we will start looking at relative to _fileSystem, _fileSystem is already scoped to the correct folder, so we use an empty string
            _targetFolder = String.Empty;

            _loggerFactory = loggerFactory;
        }

        public override IExtension FindExtension(string extensionMoniker)
        {
            // Pass 1: Require an exact case sensitive match
            IExtension exactMatch = ScanExtensionDir(extensionMoniker, StringComparison.Ordinal);
            if (exactMatch != null)
            {
                return exactMatch;
            }

            // Pass 2: Relax case sensitivity
            IExtension caseVarianceMatch = ScanExtensionDir(extensionMoniker, StringComparison.OrdinalIgnoreCase);
            if (caseVarianceMatch != null)
            {
                return caseVarianceMatch;
            }

            return null;
        }

        private IExtension ScanExtensionDir(string extensionMoniker, StringComparison comparisionType)
        {
            IDirectoryContents extensionRoot = _fileSystem.GetDirectoryContents(_targetFolder);

            string expectedFile = GetOSSpecificExecutableFormat(extensionMoniker);

            foreach (IFileInfo extDir in extensionRoot)
            {
                if (extDir.Exists && extDir.IsDirectory && string.Equals(extDir.Name, extensionMoniker, comparisionType))
                {
                    IDirectoryContents extension = _fileSystem.GetDirectoryContents(_targetFolder);
                    foreach (IFileInfo file in extension)
                    {
                        if (file.Exists && !file.IsDirectory && string.Equals(file.Name, expectedFile, comparisionType))
                        {
                            ILogger<ProgramExtension> logger = _loggerFactory.CreateLogger<ProgramExtension>();
                            return new ProgramExtension(Path.Combine(_targetFolder, extDir.Name, file.Name), logger);
                        }
                    }
                }
            }

            return null;
        }

        private string GetOSSpecificExecutableFormat(string executableName)
        {
            string format = "{0}.exe";
            string result = string.Format(CultureInfo.InvariantCulture, format, executableName);
            return result;
        }
    }
}
