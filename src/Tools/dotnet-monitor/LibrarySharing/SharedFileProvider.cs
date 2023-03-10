// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;
using System;
using System.IO;

namespace Microsoft.Diagnostics.Tools.Monitor.LibrarySharing
{
    /// <summary>
    /// An abstraction around how library files are found in the file system.
    /// </summary>
    internal sealed class SharedFileProvider : IFileProvider
    {
        private readonly string _basePath;

        private SharedFileProvider(string basePath)
        {
            _basePath = basePath;
        }

        public static IFileProvider CreateManaged(string targetFramework, string sharedLibraryPath)
        {
            return new SharedFileProvider(Path.Combine(sharedLibraryPath, "any", targetFramework));
        }

        /// <summary>
        /// Creates an <see cref="IFileProvider"/> that can return native files from the shared library layout.
        /// The path of a returned file is {sharedLibraryPath}/{runtimeIdentifier}/native/{fileName}.
        /// </summary>
        public static IFileProvider CreateNative(string runtimeIdentifier, string sharedLibraryPath)
        {
            return new SharedFileProvider(Path.Combine(sharedLibraryPath, runtimeIdentifier, "native"));
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            throw new NotSupportedException();
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            FileInfo fileInfo = new FileInfo(Path.Combine(_basePath, subpath));
            if (fileInfo.Exists)
            {
                return new PhysicalFileInfo(fileInfo);
            }
            else
            {
                return new NotFoundFileInfo(fileInfo.FullName);
            }
        }

        public IChangeToken Watch(string filter)
        {
            throw new NotSupportedException();
        }
    }
}
