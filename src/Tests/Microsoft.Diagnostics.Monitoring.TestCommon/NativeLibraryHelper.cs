﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal static class NativeLibraryHelper
    {
        private const string ConfigurationName =
#if DEBUG
            "Debug";
#else
            "Release";
#endif

        public static readonly Guid MonitorProfilerClsid = new Guid("6A494330-5848-4A23-9D87-0E57BBF6DE79");

        public static string GetMonitorProfilerPath(string architecture) =>
            GetSharedLibraryPath(architecture, "MonitorProfiler");

        private static string GetSharedLibraryPath(string architecture, string rootName)
        {
            string artifactsBinPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..", "..", ".."));
            return Path.Combine(artifactsBinPath, GetNativeBinDirectoryName(architecture), GetSharedLibraryName(rootName));
        }

        private static string GetNativeBinDirectoryName(string architecture)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return $"Windows_NT.{architecture}.{ConfigurationName}";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return $"Linux.{architecture}.{ConfigurationName}";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return $"OSX.{architecture}.{ConfigurationName}";
            }
            throw new PlatformNotSupportedException();
        }

        private static string GetSharedLibraryName(string rootName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return $"{rootName}.dll";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return $"lib{rootName}.so";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return $"lib{rootName}.dylib";
            }
            throw new PlatformNotSupportedException();
        }
    }
}
