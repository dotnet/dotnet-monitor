// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Tools.Monitor.LibrarySharing
{
    internal static class NativeLibraryHelper
    {
        public static string GetSharedLibraryName(string rootName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return FormattableString.Invariant($"{rootName}.dll");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return FormattableString.Invariant($"lib{rootName}.so");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return FormattableString.Invariant($"lib{rootName}.dylib");
            }
            throw new PlatformNotSupportedException();
        }
    }
}
