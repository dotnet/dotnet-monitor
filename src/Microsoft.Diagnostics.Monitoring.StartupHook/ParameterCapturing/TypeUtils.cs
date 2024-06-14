// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing
{
    internal static class TypeUtils
    {
        public static bool IsSubType(string parentType, string typeToCheck)
        {
            if (string.IsNullOrEmpty(parentType))
            {
                throw new ArgumentException(nameof(parentType));
            }

            if (string.IsNullOrEmpty(typeToCheck))
            {
                throw new ArgumentException(nameof(typeToCheck));
            }

            if (!typeToCheck.StartsWith(parentType, StringComparison.Ordinal))
            {
                return false;
            }

            if (typeToCheck.Length == parentType.Length)
            {
                return true;
            }

            char charAfterParentType = typeToCheck[parentType.Length];
            if (charAfterParentType == '.' || charAfterParentType == '+')
            {
                return true;
            }

            return false;
        }
    }
}
