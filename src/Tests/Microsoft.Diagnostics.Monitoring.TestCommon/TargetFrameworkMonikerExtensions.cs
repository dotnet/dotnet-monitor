// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static partial class TargetFrameworkMonikerExtensions
    {
        public static Version GetAspNetCoreFrameworkVersion(this TargetFrameworkMoniker moniker)
        {
            return ParseVersionRemoveLabel(moniker.GetAspNetCoreFrameworkVersionString());
        }

        public static string GetAspNetCoreFrameworkVersionString(this TargetFrameworkMoniker moniker)
        {
            switch (moniker)
            {
                case TargetFrameworkMoniker.Current:
                    return TestDotNetHost.CurrentAspNetCoreVersionString;
                case TargetFrameworkMoniker.NetCoreApp31:
                    return TestDotNetHost.AspNetCore31VersionString;
                case TargetFrameworkMoniker.Net50:
                    return TestDotNetHost.AspNetCore50VersionString;
                case TargetFrameworkMoniker.Net60:
                    return TestDotNetHost.AspNetCore60VersionString;
                case TargetFrameworkMoniker.Net70:
                    return TestDotNetHost.AspNetCore70VersionString;
                case TargetFrameworkMoniker.Net80:
                    return TestDotNetHost.AspNetCore80VersionString;
            }
            throw CreateUnsupportedException(moniker);
        }

        public static string GetNetCoreAppFrameworkVersionString(this TargetFrameworkMoniker moniker)
        {
            switch (moniker)
            {
                case TargetFrameworkMoniker.Current:
                    return TestDotNetHost.CurrentNetCoreVersionString;
                case TargetFrameworkMoniker.NetCoreApp31:
                    return TestDotNetHost.NetCore31VersionString;
                case TargetFrameworkMoniker.Net50:
                    return TestDotNetHost.NetCore50VersionString;
                case TargetFrameworkMoniker.Net60:
                    return TestDotNetHost.NetCore60VersionString;
                case TargetFrameworkMoniker.Net70:
                    return TestDotNetHost.NetCore70VersionString;
                case TargetFrameworkMoniker.Net80:
                    return TestDotNetHost.NetCore80VersionString;
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

        private static Version ParseVersionRemoveLabel(string versionString)
        {
            Assert.NotNull(versionString);
            int prereleaseLabelIndex = versionString.IndexOf('-');
            if (prereleaseLabelIndex >= 0)
            {
                versionString = versionString.Substring(0, prereleaseLabelIndex);
            }
            return Version.Parse(versionString);
        }
    }
}
