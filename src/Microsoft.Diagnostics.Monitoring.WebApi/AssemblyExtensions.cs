// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class AssemblyExtensions
    {
        public static string GetInformationalVersionString(this Assembly assembly)
        {
            if (assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                is AssemblyInformationalVersionAttribute assemblyVersionAttribute)
            {
                return assemblyVersionAttribute.InformationalVersion;
            }
            else
            {
                return assembly.GetName().Version.ToString();
            }
        }
    }
}
