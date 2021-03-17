// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class EnvironmentInformation
    {
        private static readonly Lazy<bool> _isElevatedLazy =
            new Lazy<bool>(GetIsElevated);

        public static bool IsElevated => _isElevatedLazy.Value;

        private static bool GetIsElevated()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(currentUser);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }

            // TODO: Check on Linux/MacOS?
            return false;
        }
    }
}
