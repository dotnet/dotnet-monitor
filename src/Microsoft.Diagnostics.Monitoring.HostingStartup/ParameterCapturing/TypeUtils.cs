// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    internal static class TypeUtils
    {
        public static bool DoesBelongToScope(string scopeName, string typeToCheck)
        {
            if (string.IsNullOrEmpty(scopeName))
            {
                throw new ArgumentException(nameof(scopeName));
            }

            if (string.IsNullOrEmpty(typeToCheck))
            {
                throw new ArgumentException(nameof(typeToCheck));
            }

            if (!typeToCheck.StartsWith(scopeName, StringComparison.Ordinal))
            {
                return false;
            }

            if (typeToCheck.Length == scopeName.Length)
            {
                return true;
            }

            char charAfterNamespace = typeToCheck[scopeName.Length];
            if (charAfterNamespace == '.' || charAfterNamespace == '+')
            {
                return true;
            }

            return false;
        }
    }
}
