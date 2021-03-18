// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Runtime.InteropServices;
using Xunit.Extensions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class Skip
    {
        public static void IfNotPlatform(params OSPlatform[] platforms)
        {
            if (!platforms.Any(p => RuntimeInformation.IsOSPlatform(p)))
            {
                string platformsString = string.Join(",", platforms.Select(p => p.ToString()));
                throw new SkipTestException($"Test only runs on the following platfoms: {platformsString}");
            }
        }
    }
}
