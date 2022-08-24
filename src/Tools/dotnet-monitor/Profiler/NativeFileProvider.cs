// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System.IO;
using System;
using System.Reflection;

namespace Microsoft.Diagnostics.Tools.Monitor.Profiler
{
    internal sealed class NativeFileProvider : IFileProvider
    {
        private readonly string _nativeFileBasePath;

        private NativeFileProvider(string osPlatform, string nativeFileBasePath)
        {
            _nativeFileBasePath = nativeFileBasePath;
        }

        public static IFileProvider CreateShared(string runtimeIdentifier, string sharedLibraryPath)
        {
            SplitRuntimeIdentifier(runtimeIdentifier, out string osPlatform, out string architecture);

            return new NativeFileProvider(osPlatform, Path.Combine(sharedLibraryPath, runtimeIdentifier, "native"));
        }

        public static IFileProvider CreateTest(string runtimeIdentifier)
        {
            SplitRuntimeIdentifier(runtimeIdentifier, out string osPlatform, out string architecture);

            string nativePlatformFolderPrefix = null;
            switch (osPlatform)
            {
                case "linux":
                case "linux-musl":
                    nativePlatformFolderPrefix = "Linux";
                    break;
                case "osx":
                    nativePlatformFolderPrefix = "OSX";
                    break;
                case "win":
                    nativePlatformFolderPrefix = "Windows_NT";
                    break;
                default:
                    throw new PlatformNotSupportedException();
            }

            string configurationName =
#if DEBUG
            "Debug";
#else
            "Release";
#endif

        string nativeOutputPath = Path.Combine(
                Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..", "..", "..")),
                $"{nativePlatformFolderPrefix}.{architecture}.{configurationName}");

            return new NativeFileProvider(osPlatform, nativeOutputPath);
        }

        private static void SplitRuntimeIdentifier(string runtimeIdentifier, out string osPlatform, out string architecture)
        {
            int index = runtimeIdentifier.LastIndexOf('-');
            osPlatform = runtimeIdentifier.Substring(0, index);
            architecture = runtimeIdentifier.Substring(index + 1);
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
