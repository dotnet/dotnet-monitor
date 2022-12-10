// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class DumpUtilities
    {
        public static string GenerateDumpFileName()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                FormattableString.Invariant($"dump_{Utilities.GetFileNameTimeStampUtcNow()}.dmp") :
                FormattableString.Invariant($"core_{Utilities.GetFileNameTimeStampUtcNow()}");
        }
    }
}
