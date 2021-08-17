// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class AssemblyHelper
    {
        public static string GetAssemblyArtifactBinPath(
            Assembly testAssembly,
            string assemblyName,
            TargetFrameworkMoniker tfm = TargetFrameworkMoniker.Current)
        {
            string assemblyPath = testAssembly.Location
                .Replace(testAssembly.GetName().Name, assemblyName);

            if (tfm != TargetFrameworkMoniker.Current)
            {
                string currentFolderName = GetTargetFrameworkMonikerFolderName(DotNetHost.BuiltTargetFrameworkMoniker);
                string targetFolderName = GetTargetFrameworkMonikerFolderName(tfm);

                assemblyPath = assemblyPath.Replace(currentFolderName, targetFolderName);
            }

            return assemblyPath;
        }

        private static string GetTargetFrameworkMonikerFolderName(TargetFrameworkMoniker moniker)
        {
            switch (moniker)
            {
                case TargetFrameworkMoniker.Net50:
                    return "net5.0";
                case TargetFrameworkMoniker.Net60:
                    return "net6.0";
                case TargetFrameworkMoniker.NetCoreApp31:
                    return "netcoreapp3.1";
            }
            throw new ArgumentException($"Moniker '{moniker:G}' is not supported.");
        }
    }
}
