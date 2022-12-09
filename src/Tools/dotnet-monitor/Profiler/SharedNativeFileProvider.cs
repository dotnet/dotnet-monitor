// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;
using System;
using System.IO;

namespace Microsoft.Diagnostics.Tools.Monitor.Profiler
{
    /// <summary>
    /// An abstraction around how native library files are found in the file system.
    /// </summary>
    internal sealed class SharedNativeFileProvider : IFileProvider
    {
        private readonly string _nativeFileBasePath;

        private SharedNativeFileProvider(string nativeFileBasePath)
        {
            _nativeFileBasePath = nativeFileBasePath;
        }

        /// <summary>
        /// Creates an <see cref="IFileProvider"/> that can return native files from the shared library layout.
        /// The path of a returned file is {sharedLibraryPath}/{runtimeIdentifier}/native/{fileName}.
        /// </summary>
        public static IFileProvider Create(string runtimeIdentifier, string sharedLibraryPath)
        {
            return new SharedNativeFileProvider(Path.Combine(sharedLibraryPath, runtimeIdentifier, "native"));
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            throw new NotSupportedException();
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            FileInfo fileInfo = new FileInfo(Path.Combine(_nativeFileBasePath, subpath));
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
