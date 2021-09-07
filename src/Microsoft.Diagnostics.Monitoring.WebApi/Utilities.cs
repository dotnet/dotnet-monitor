// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    public class Utilities
    {
        internal static string GenerateDumpFileName()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                FormattableString.Invariant($"dump_{GetFileNameTimeStampUtcNow()}.dmp") :
                FormattableString.Invariant($"core_{GetFileNameTimeStampUtcNow()}");
        }

        internal static string GetFileNameTimeStampUtcNow()
        {
            return DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        }
    }
}
