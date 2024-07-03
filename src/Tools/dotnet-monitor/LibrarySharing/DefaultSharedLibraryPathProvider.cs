// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Reflection;

namespace Microsoft.Diagnostics.Tools.Monitor.LibrarySharing
{
    internal sealed class DefaultSharedLibraryPathProvider
    {
        /// <summary>
        /// Provides libraries from the 'shared' folder under the .NET Monitor installation.
        /// </summary>
        #nullable disable
        public DefaultSharedLibraryPathProvider(IOptions<StorageOptions> _storageOptions)
            : this(Path.Combine(AppContext.BaseDirectory, "shared"), ComputeTargetPath(_storageOptions))
        {
        }
#nullable restore

        public DefaultSharedLibraryPathProvider(string sourcePath, string targetPath)
        {
            SourcePath = sourcePath ?? throw new ArgumentNullException(nameof(sourcePath));
            TargetPath = targetPath;
        }

        private static string? ComputeTargetPath(IOptions<StorageOptions> options)
        {
            string? rootPath = options.Value.SharedLibraryPath;
            if (string.IsNullOrEmpty(rootPath))
                return null;

            string? expectedVersion = Assembly.GetExecutingAssembly()?.GetInformationalVersionString();
            if (expectedVersion == null)
                return null;

            // Remove '+' and commit hash from version string
            int hashSeparatorIndex = expectedVersion.IndexOf('+');
            if (hashSeparatorIndex > 0)
            {
                expectedVersion = expectedVersion.Substring(0, hashSeparatorIndex);
            }

            // If the user specified '/diag/libs' for Storage:SharedLibraryPath, this path may look like '/diag/libs/7.0.0'
            // This path includes the .NET Monitor version in order to avoid collisions when performing rolling updates
            // in containerized environments; newer .NET Monitor versions must repopulate the shared library directory, but
            // do not assume that the prior versions are removable due to possible file locks.
            return Path.Combine(rootPath, expectedVersion);
        }

        public string SourcePath { get; }

        public string TargetPath { get; }
    }
}
