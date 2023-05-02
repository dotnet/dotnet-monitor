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
    /// An abstraction around how managed library files are found in the build output.
    /// </summary>
    internal sealed class BuildOutputManagedFileProvider : IFileProvider
    {
        private readonly string _managedFileBasePath;
        private readonly string _targetFramework;

        private BuildOutputManagedFileProvider(string managedFileBasePath, string targetFramework)
        {
            _managedFileBasePath = managedFileBasePath;
            _targetFramework = targetFramework;
        }

        /// <summary>
        /// Creates an <see cref="IFileProvider"/> that can return native files from the build output of a
        /// local or CI build from the dotnet-monitor repository.
        /// The path of a returned file is {sharedLibraryPath}/{libraryName}/{configuration}/{targetFramework}/{fileName}.
        /// </summary>
        public static IFileProvider Create(string targetFramework, string sharedLibraryPath)
        {
            return new BuildOutputManagedFileProvider(sharedLibraryPath, targetFramework);
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            throw new NotSupportedException();
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            string libraryName = Path.GetFileNameWithoutExtension(subpath);
            FileInfo fileInfo = new FileInfo(Path.Combine(_managedFileBasePath, libraryName, BuildOutput.ConfigurationName, _targetFramework, subpath));
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
