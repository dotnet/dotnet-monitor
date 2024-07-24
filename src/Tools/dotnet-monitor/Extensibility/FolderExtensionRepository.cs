// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.Diagnostics.Tools.Monitor.Egress.Extension;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal class FolderExtensionRepository : ExtensionRepository
    {
        private readonly EgressExtensionFactory _egressExtensionFactory;
        private readonly IFileProvider _fileSystem;
        private readonly ILogger<FolderExtensionRepository> _logger;

        public FolderExtensionRepository(IFileProvider fileSystem, EgressExtensionFactory egressExtensionFactory, ILogger<FolderExtensionRepository> logger)
        {
            _egressExtensionFactory = egressExtensionFactory;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public override bool TryFindExtension(string extensionName, out IExtension extension)
        {
            IDirectoryContents extensionDirs = _fileSystem.GetDirectoryContents(string.Empty);

            foreach (IFileInfo extensionDir in extensionDirs)
            {
                if (extensionDir.IsDirectory && !string.IsNullOrEmpty(extensionDir.PhysicalPath))
                {
                    string manifestPath = Path.Combine(extensionDir.PhysicalPath, ExtensionManifest.DefaultFileName);

                    ExtensionManifest manifest;
                    try
                    {
                        manifest = ExtensionManifest.FromPath(manifestPath);
                    }
                    catch (Exception ex) when (LogManifestParseError(ex, manifestPath))
                    {
                        continue;
                    }

                    if (extensionName == manifest.Name)
                    {
                        extension = _egressExtensionFactory.Create(manifest, extensionDir.PhysicalPath);
                        return true;
                    }
                }
            }

            extension = null;
            return false;
        }

        private bool LogManifestParseError(Exception ex, string manifestPath)
        {
            _logger.ExtensionManifestNotParsable(manifestPath, ex);
            return true;
        }
    }
}
