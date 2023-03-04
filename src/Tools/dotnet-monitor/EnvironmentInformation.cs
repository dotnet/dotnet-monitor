// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    public static class EnvironmentInformation
    {
        private static readonly Lazy<bool> _isElevatedLazy = new(GetIsElevated);

        public static bool IsElevated => _isElevatedLazy.Value;

        private static bool GetIsElevated()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new(currentUser);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }

            // TODO: Check on Linux/MacOS?
            return false;
        }
    }
}
