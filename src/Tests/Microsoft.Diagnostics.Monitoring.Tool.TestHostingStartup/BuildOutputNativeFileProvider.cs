// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;
using System;
using System.IO;

namespace Microsoft.Diagnostics.Monitoring.Tool.TestHostingStartup
{
    /// <summary>
    /// An abstraction around how native library files are found in the build output.
    /// </summary>
    internal sealed class BuildOutputNativeFileProvider : IFileProvider
    {
        private readonly string _nativeFileBasePath;

        private BuildOutputNativeFileProvider(string nativeFileBasePath)
        {
            _nativeFileBasePath = nativeFileBasePath;
        }

        /// <summary>
        /// Creates an <see cref="IFileProvider"/> that can return native files from the build output of a
        /// local or CI build from the dotnet-monitor repository.
        /// The path of a returned file is {sharedLibraryPath}/{nativePlatformFolder}/{fileName}.
        /// </summary>
        public static IFileProvider Create(string runtimeIdentifier, string sharedLibraryPath)
        {
            string nativeOutputPath = Path.Combine(sharedLibraryPath, $"{runtimeIdentifier}.{BuildOutput.ConfigurationName}");

            return new BuildOutputNativeFileProvider(nativeOutputPath);
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
