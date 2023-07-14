// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32.SafeHandles;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Cryptography;

namespace Microsoft.Diagnostics.Tools.Monitor.LibrarySharing
{
    internal sealed class DefaultSharedLibraryInitializer :
        ISharedLibraryInitializer,
        IDisposable
    {
        private static readonly string SharedLibrarySourcePath = Path.Combine(AppContext.BaseDirectory, "shared");

        private readonly List<SafeFileHandle> _sharedFileHandles = new();
        private readonly ILogger<DefaultSharedLibraryInitializer> _logger;
        private readonly string _sharedLibraryTargetPath;

        private long _disposeState;

        public DefaultSharedLibraryInitializer(
            IOptions<StorageOptions> _storageOptions,
            ILogger<DefaultSharedLibraryInitializer> logger)
        {
            _logger = logger;
            _sharedLibraryTargetPath = _storageOptions.Value.SharedLibraryPath;
        }

        public void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposeState))
                return;

            // Release locks on shared file handles
            foreach (SafeFileHandle handle in _sharedFileHandles)
            {
                handle.Dispose();
            }
            _sharedFileHandles.Clear();
        }

        public IFileProviderFactory Initialize()
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

                Queue<string> subDirectories = new();
                subDirectories.Enqueue(string.Empty); // The root of the shared library source directory
                using SHA256 hasher = SHA256.Create();
                Span<byte> sourceHash = stackalloc byte[32];
                Span<byte> targetHash = stackalloc byte[32];

                // Go through each directory and validate the existing files are exactly the same as expected
                // or create new files if they do not exist. Hold onto the file handles to prevent modification
                // of the shared files while they are being offered for use in target applications.
                while (subDirectories.TryDequeue(out string subDirectory))
                {
                    DirectoryInfo sourceDir = new(Path.Combine(sharedLibrarySourceDir.FullName, subDirectory));
                    DirectoryInfo targetDir = Directory.CreateDirectory(Path.Combine(versionedSharedLibraryTargetPath, subDirectory));

                    FileInfo[] sourceFiles = sourceDir.GetFiles();
                    for (int i = 0; i < sourceFiles.Length; i++)
                    {
                        string targetFilePath = Path.Combine(targetDir.FullName, sourceFiles[i].Name);
                        if (File.Exists(targetFilePath))
                        {
                            string sourceFilePath = sourceFiles[i].FullName;
                            using SafeFileHandle sourceHandle = File.OpenHandle(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                            long sourceLength = RandomAccess.GetLength(sourceHandle);

                            // Open target file handle for reading and only allow read sharing to prevent modification of file while in use
                            SafeFileHandle targetHandle = File.OpenHandle(targetFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                            _sharedFileHandles.Add(targetHandle);
                            long targetLength = RandomAccess.GetLength(targetHandle);

                            // Check that they are the same length
                            if (targetLength != sourceLength)
                            {
                                throw new InvalidOperationException(
                                    string.Format(
                                        CultureInfo.InvariantCulture,
                                        Strings.ErrorMessage_SharedFileDiffersFromSource,
                                        targetFilePath,
                                        sourceFilePath));
                            }

                            // Check that they have the same hash value
                            ComputeHash(sourceHandle, sourceHash);
                            ComputeHash(targetHandle, targetHash);

                            if (!sourceHash.SequenceEqual(targetHash))
                            {
                                throw new InvalidOperationException(
                                    string.Format(
                                        CultureInfo.InvariantCulture,
                                        Strings.ErrorMessage_SharedFileDiffersFromSource,
                                        targetFilePath,
                                        sourceFilePath));
                            }
                        }
                        else
                        {
                            // Open target file handle for writing and only allow read sharing to prevent modification of file while in use
                            SafeFileHandle targetHandle = File.OpenHandle(targetFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read, FileOptions.WriteThrough);
                            _sharedFileHandles.Add(targetHandle);
                            RandomAccess.Write(targetHandle, File.ReadAllBytes(sourceFiles[i].FullName), 0);
                        }
                    }

                    DirectoryInfo[] childDirs = sourceDir.GetDirectories();
                    for (int i = 0; i < childDirs.Length; i++)
                    {
                        subDirectories.Enqueue(Path.Combine(subDirectory, childDirs[i].Name));
                    }
                }

                sharedLibraryPath = versionedSharedLibraryTargetPath;
            }

            _logger.SharedLibraryPath(sharedLibraryPath);

            return new Factory(sharedLibraryPath);
        }

        private static void ComputeHash(SafeFileHandle handle, Span<byte> destination)
        {
            int length = (int)RandomAccess.GetLength(handle);
            byte[] content = ArrayPool<byte>.Shared.Rent(length);
            try
            {
                RandomAccess.Read(handle, content, 0);
                SHA256.HashData(content, destination);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(content);
            }
        }

        private sealed class Factory : IFileProviderFactory
        {
            private readonly string _sharedLibraryPath;

            public Factory(string sharedLibraryPath)
            {
                _sharedLibraryPath = sharedLibraryPath;
            }

            public IFileProvider CreateManaged(string targetFramework)
            {
                return SharedFileProvider.CreateManaged(targetFramework, _sharedLibraryPath);
            }

            public IFileProvider CreateNative(string runtimeIdentifier)
            {
                return SharedFileProvider.CreateNative(runtimeIdentifier, _sharedLibraryPath);
            }
        }
    }
}
