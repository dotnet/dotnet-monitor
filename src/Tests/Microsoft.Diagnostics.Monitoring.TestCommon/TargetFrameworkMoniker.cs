﻿// Licensed to the .NET Foundation under one or more agreements.
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

        // Checks if the specified moniker is the same as the test value or if it is Current
        // then matches the same TFM for which this assembly was built.
        public static bool IsEffectively(this TargetFrameworkMoniker moniker, TargetFrameworkMoniker test)
        {
            if (TargetFrameworkMoniker.Current == test)
            {
                throw new ArgumentException($"Parameter {nameof(test)} cannot be TargetFrameworkMoniker.Current");
            }

            return moniker == test || (TargetFrameworkMoniker.Current == moniker && CurrentTargetFrameworkMoniker == test);
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

        private static readonly TargetFrameworkMoniker CurrentTargetFrameworkMoniker =
#if NET6_0
            TargetFrameworkMoniker.Net60;
#elif NET5_0
            TargetFrameworkMoniker.Net50;
#elif NETCOREAPP3_1
            TargetFrameworkMoniker.NetCoreApp31;
#endif
    }
}
