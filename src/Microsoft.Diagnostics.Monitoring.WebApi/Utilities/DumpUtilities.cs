// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class DumpUtilities
    {
        public static string GenerateDumpFileName(Models.PackageMode mode = Models.PackageMode.None)
        {
            string file = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dump" : "core";
            string extension = mode.HasFlag(Models.PackageMode.DiagSession) ? ".diagsession" : RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".dmp" : string.Empty;
            return FormattableString.Invariant($"{file}_{Utilities.GetFileNameTimeStampUtcNow()}{extension}");
        }
    }
}
