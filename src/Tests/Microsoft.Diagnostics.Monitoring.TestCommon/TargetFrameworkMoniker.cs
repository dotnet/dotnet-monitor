// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public enum TargetFrameworkMoniker
    {
        Current,
        NetCoreApp31,
        Net50,
        Net60,
        Net70,
        Net80
    }

    public static partial class TargetFrameworkMonikerExtensions
    {
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
                case TargetFrameworkMoniker.Net70:
                    return "net7.0";
                case TargetFrameworkMoniker.Net80:
                    return "net8.0";
            }
            throw CreateUnsupportedException(moniker);
        }

        private static ArgumentException CreateUnsupportedException(TargetFrameworkMoniker moniker)
        {
            return new ArgumentException($"Unsupported target framework moniker: {moniker:G}");
        }

        public const TargetFrameworkMoniker CurrentTargetFrameworkMoniker =
#if NET8_0
            TargetFrameworkMoniker.Net80;
#elif NET7_0
            TargetFrameworkMoniker.Net70;
#elif NET6_0
            TargetFrameworkMoniker.Net60;
#elif NET5_0
            TargetFrameworkMoniker.Net50;
#elif NETCOREAPP3_1
            TargetFrameworkMoniker.NetCoreApp31;
#endif
    }
}
