// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                string currentFolderName = TestDotNetHost.BuiltTargetFrameworkMoniker.ToFolderName();
                string targetFolderName = tfm.ToFolderName();

                assemblyPath = assemblyPath.Replace(currentFolderName, targetFolderName);
            }

            return assemblyPath;
        }
    }
}
