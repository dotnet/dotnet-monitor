// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class DumpUtilities
    {
        public const string ArtifactType_Dump = "dump";

        public static string GenerateDumpFileName()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                FormattableString.Invariant($"dump_{Utilities.GetFileNameTimeStampUtcNow()}.dmp") :
                FormattableString.Invariant($"core_{Utilities.GetFileNameTimeStampUtcNow()}");
        }
    }
}