// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public sealed class TargetFrameworkMonikerComparer : Comparer<TargetFrameworkMoniker>
    {
        public static new IComparer<TargetFrameworkMoniker> Default = new TargetFrameworkMonikerComparer();

        public override int Compare(TargetFrameworkMoniker x, TargetFrameworkMoniker y)
        {
            if (x == y)
            {
                return 0;
            }

            // This assumes that the enum values or ordered from lowest to higher version.
            return ((int)x).CompareTo((int)y);
        }
    }
}