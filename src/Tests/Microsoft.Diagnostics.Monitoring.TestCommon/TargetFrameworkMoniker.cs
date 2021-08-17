// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public enum TargetFrameworkMoniker
    {
        Current,
        NetCoreApp31,
        Net50,
        Net60
    }

    public static class TargetFrameworkMonikerExtensions
    {
        public static string GetAspNetCoreFrameworkVersion(this TargetFrameworkMoniker moniker)
        {
            switch (moniker)
            {
                case TargetFrameworkMoniker.Current:
                    return DotNetHost.CurrentAspNetCoreVersionString;
            }
            throw CreateUnsupportedException(moniker);
        }

        public static string GetNetCoreAppFrameworkVersion(this TargetFrameworkMoniker moniker)
        {
            switch (moniker)
            {
                case TargetFrameworkMoniker.Current:
                    return DotNetHost.CurrentNetCoreVersionString;
                case TargetFrameworkMoniker.NetCoreApp31:
                    return DotNetHost.NetCore31VersionString;
                case TargetFrameworkMoniker.Net50:
                    return DotNetHost.NetCore50VersionString;
                case TargetFrameworkMoniker.Net60:
                    return DotNetHost.NetCore60VersionString;
            }
            throw CreateUnsupportedException(moniker);
        }

        public static string ToFolderName(this TargetFrameworkMoniker moniker)
        {
            switch (moniker)
            {
                case TargetFrameworkMoniker.Net50:
                    return "net5.0";
                case TargetFrameworkMoniker.NetCoreApp31:
                    return "netcoreapp3.1";
                case TargetFrameworkMoniker.Net60:
                    return "net6.0";
            }
            throw CreateUnsupportedException(moniker);
        }

        private static ArgumentException CreateUnsupportedException(TargetFrameworkMoniker moniker)
        {
            return new ArgumentException($"Unsupported target framework moniker: {moniker:G}");
        }
    }
}
