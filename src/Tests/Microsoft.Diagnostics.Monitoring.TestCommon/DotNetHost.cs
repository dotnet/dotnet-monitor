// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public partial class DotNetHost
    {
        // The version is in the Major.Minor.Patch-label format; remove the label
        // and only parse the Major.Minor.Patch part.
        private static Lazy<Version> s_runtimeVersionLazy =
            new(() => Version.Parse(CurrentNetCoreVersionString.Split("-")[0]));

        public static Version RuntimeVersion =>
            s_runtimeVersionLazy.Value;

        public static string HostExeNameWithoutExtension => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            Path.GetFileNameWithoutExtension(HostExePath) :
            Path.GetFileName(HostExePath);

        public static string HostExePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            @"..\..\..\..\..\.dotnet\dotnet.exe" :
            "../../../../../.dotnet/dotnet";

        public static TargetFrameworkMoniker BuiltTargetFrameworkMoniker
        {
            get
            {
                // Update with specific TFM when building this assembly for a new target framework.
#if NETCOREAPP3_1
                return TargetFrameworkMoniker.NetCoreApp31;
#elif NET5_0
                return TargetFrameworkMoniker.Net50;
#elif NET6_0
                return TargetFrameworkMoniker.Net60;
#endif
            }
        }
    }
}
