// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.LibrarySharing
{
    internal sealed class DefaultSharedLibraryInitializer :
        ISharedLibraryInitializer,
        IDisposable
    {
        private readonly List<SafeFileHandle> _sharedFileHandles = new();
        private readonly ILogger<DefaultSharedLibraryInitializer> _logger;
        private readonly DefaultSharedLibraryPathProvider _pathProvider;

        private long _disposeState;

        public DefaultSharedLibraryInitializer(
            DefaultSharedLibraryPathProvider pathProvider,
            ILogger<DefaultSharedLibraryInitializer> logger)
        {
            _logger = logger;
            _pathProvider = pathProvider;
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

        public async Task<IFileProviderFactory> InitializeAsync(CancellationToken cancellationToken)
        {
            // Copy the shared libraries to the path specified by Storage:SharedLibraryPath.
            // Copying, instead of linking or using them in-place, prevents file locks from the target process.
            // If shared path is not specified, use the libraries in-place.
            DirectoryInfo sharedLibrarySourceDir = new DirectoryInfo(_pathProvider.SourcePath);
            if (!sharedLibrarySourceDir.Exists)
            {
                throw new DirectoryNotFoundException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Strings.ErrorMessage_ExpectedToFindSharedLibrariesAtPath,
                        _pathProvider.SourcePath));
            }

            string sharedLibraryPath;
            if (string.IsNullOrEmpty(_pathProvider.TargetPath))
            {
                // Since Storage:SharedLibraryPath was not specified, allow providing shared libraries directly
                // from the source path.
                sharedLibraryPath = _pathProvider.SourcePath;
            }
            else
            {
                Queue<string> subDirectories = new();
                subDirectories.Enqueue(string.Empty); // The root of the shared library source directory

                // Go through each directory and validate the existing files are exactly the same as expected
                // or create new files if they do not exist. Hold onto the file handles to prevent modification
                // of the shared files while they are being offered for use in target applications.
                while (subDirectories.TryDequeue(out string? subDirectory))
                {
                    DirectoryInfo sourceDir = new(Path.Combine(sharedLibrarySourceDir.FullName, subDirectory));
                    DirectoryInfo targetDir = Directory.CreateDirectory(Path.Combine(_pathProvider.TargetPath, subDirectory));

                    FileInfo[] sourceFiles = sourceDir.GetFiles();
                    for (int i = 0; i < sourceFiles.Length; i++)
                    {
                        string targetFilePath = Path.Combine(targetDir.FullName, sourceFiles[i].Name);
                        if (File.Exists(targetFilePath))
                        {
                            string sourceFilePath = sourceFiles[i].FullName;
                            using SafeFileHandle sourceHandle = File.OpenHandle(
                                sourceFilePath,
                                FileMode.Open,
                                FileAccess.Read,
                                FileShare.Read,
                                FileOptions.Asynchronous | FileOptions.SequentialScan);
                            long sourceLength = RandomAccess.GetLength(sourceHandle);

                            // Open target file handle for reading and only allow read sharing to prevent modification of file while in use
                            SafeFileHandle targetHandle = File.OpenHandle(
                                targetFilePath,
                                FileMode.Open,
                                FileAccess.Read,
                                FileShare.Read,
                                FileOptions.Asynchronous | FileOptions.SequentialScan);
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

                            // Check that they have the same content
                            using FileStream sourceStream = new(sourceHandle, FileAccess.Read, StreamDefaults.BufferSize, isAsync: true);
                            using FileStream targetStream = new(targetHandle, FileAccess.Read, StreamDefaults.BufferSize, isAsync: true);

                            if (!await sourceStream.HasSameContentAsync(targetStream, cancellationToken))
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
                            SafeFileHandle targetHandle = File.OpenHandle(
                                targetFilePath,
                                FileMode.CreateNew,
                                FileAccess.Write,
                                FileShare.Read,
                                FileOptions.WriteThrough | FileOptions.Asynchronous);
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

                sharedLibraryPath = _pathProvider.TargetPath;
            }

            _logger.SharedLibraryPath(sharedLibraryPath);

            return new Factory(sharedLibraryPath);
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
