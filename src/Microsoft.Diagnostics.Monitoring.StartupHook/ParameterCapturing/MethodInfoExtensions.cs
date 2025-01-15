// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing
{
    internal static class MethodInfoExtensions
    {
        public static ulong GetFunctionId(this MethodInfo method)
        {
            return (ulong)method.MethodHandle.Value.ToInt64();
        }

        public static bool HasImplicitThis(this MethodInfo method)
        {
            return method.CallingConvention.HasFlag(CallingConventions.HasThis);
        }

        public static bool DoesBelongToType(this MethodInfo method, string parentType)
        {
            if (method.DeclaringType == null || method.DeclaringType.FullName == null)
            {
                return false;
            }

            return TypeUtils.IsSubType(parentType, method.DeclaringType.FullName);
        }
    }
}
