// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    internal static class MethodInfoExtensions
    {
        public static ulong GetFunctionId(this MethodInfo method)
        {
            return (ulong)method.MethodHandle.Value.ToInt64();
        }
    }
}
