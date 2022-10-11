﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace Microsoft.Diagnostics.Tools.Monitor.Profiler
{
    internal sealed class DefaultSharedLibraryInitializer :
        ISharedLibraryInitializer
    {
        private static readonly string SharedLibrarySourcePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "shared");

        private readonly ILogger<DefaultSharedLibraryInitializer> _logger;
        private readonly string _sharedLibraryTargetPath;

        public DefaultSharedLibraryInitializer(
            IOptions<StorageOptions> _storageOptions,
            ILogger<DefaultSharedLibraryInitializer> logger)
        {
            _logger = logger;
            _sharedLibraryTargetPath = _storageOptions.Value.SharedLibraryPath;
        }

        public INativeFileProviderFactory Initialize()
        {
            // Copy the shared libraries to the path specified by Storage:SharedLibraryPath.
            // Copying, instead of linking or using them in-place, prevents file locks from the target process.
            // If shared path is not specified, use the libraries in-place.
            DirectoryInfo sharedLibrarySourceDir = new DirectoryInfo(SharedLibrarySourcePath);
            if (!sharedLibrarySourceDir.Exists)
            {
                throw new DirectoryNotFoundException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Strings.ErrorMessage_ExpectedToFindSharedLibrariesAtPath,
                        SharedLibrarySourcePath));
            }

            string sharedLibraryPath;
            if (string.IsNullOrEmpty(_sharedLibraryTargetPath))
            {
                // Since Storage:SharedLibraryPath was not specified, allow providing shared libraries directly
                // from the 'shared' folder in the dotnet-monitor package.
                sharedLibraryPath = SharedLibrarySourcePath;
            }
            else
            {
                string expectedVersion = Assembly.GetExecutingAssembly().GetInformationalVersionString();
                // Remove '+' and commit hash from version string
                int hashSeparatorIndex = expectedVersion.IndexOf('+');
                if (hashSeparatorIndex > 0)
                {
                    expectedVersion = expectedVersion.Substring(0, hashSeparatorIndex);
                }

                // This is the target location to where shared libraries will be copied.
                // If the user specified '/diag/libs' for Storage:SharedLibraryPath, this path may look like '/diag/libs/7.0.0'
                // This path includes the dotnet-monitor version in order to avoid collisions when performing rolling updates
                // in containerized environments; newer dotnet-monitor versions must repopulate the shared library directory, but
                // do not assume that the prior versions are deletable due to possible file locks.
                string versionedSharedLibraryTargetPath = Path.Combine(_sharedLibraryTargetPath, expectedVersion);

                // Check that the sentinel file exists (which would indicate this version of the libraries was staged correctly).
                string sentinelPath = Path.Combine(versionedSharedLibraryTargetPath, "completed");
                if (!File.Exists(sentinelPath))
                {
                    sharedLibrarySourceDir.CopyContentsTo(Directory.CreateDirectory(versionedSharedLibraryTargetPath), overwrite: true);

                    // Write a sentinel file to signal that staging of the directory completed. The lack of
                    // this file signals that directory was partially staged and must be restaged.
                    File.Create(sentinelPath).Dispose();
                }

                sharedLibraryPath = versionedSharedLibraryTargetPath;
            }

            _logger.SharedLibraryPath(sharedLibraryPath);

            return new Factory(sharedLibraryPath);
        }

        private sealed class Factory : INativeFileProviderFactory
        {
            private readonly string _sharedLibraryPath;

            public Factory(string sharedLibraryPath)
            {
                _sharedLibraryPath = sharedLibraryPath;
            }

            public IFileProvider Create(string runtimeIdentifier)
            {
                return SharedNativeFileProvider.Create(runtimeIdentifier, _sharedLibraryPath);
            }
        }
    }
}
