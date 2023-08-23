// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    internal static class TypeUtils
    {
        public static bool DoesBelongToNamespace(string namespaceName, string typeToCheck)
        {
            if (string.IsNullOrEmpty(namespaceName))
            {
                throw new ArgumentException(nameof(namespaceName));
            }

            if (string.IsNullOrEmpty(typeToCheck))
            {
                throw new ArgumentException(nameof(typeToCheck));
            }

            if (!typeToCheck.StartsWith(namespaceName, StringComparison.Ordinal))
            {
                return false;
            }

            if (typeToCheck.Length == namespaceName.Length)
            {
                return true;
            }

            char charAfterNamespace = typeToCheck[namespaceName.Length];
            if (charAfterNamespace == '.' || charAfterNamespace == '+')
            {
                return true;
            }

            return true;
        }
    }
}
