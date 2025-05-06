// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public enum TargetFrameworkMoniker
    {
        Current,
        [Obsolete("Do not use except for startup hook.")]
        Net60,
        Net80,
        Net90
    }

    public static partial class TargetFrameworkMonikerExtensions
    {
        public static string ToFolderName(this TargetFrameworkMoniker moniker)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            switch (moniker)
            {
                case TargetFrameworkMoniker.Net60:
                    return "net6.0";
                case TargetFrameworkMoniker.Net80:
                    return "net8.0";
                case TargetFrameworkMoniker.Net90:
                    return "net9.0";
            }
#pragma warning restore CS0618 // Type or member is obsolete
            throw CreateUnsupportedException(moniker);
        }

        private static ArgumentException CreateUnsupportedException(TargetFrameworkMoniker moniker)
        {
            return new ArgumentException($"Unsupported target framework moniker: {moniker:G}");
        }

        public const TargetFrameworkMoniker CurrentTargetFrameworkMoniker =
#if NET9_0
            TargetFrameworkMoniker.Net90;
#elif NET8_0
            TargetFrameworkMoniker.Net80;
#endif
    }
}
