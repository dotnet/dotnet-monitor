﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/* Unmerged change from project 'dotnet-monitor(net6.0)'
Before:
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
After:
using System.Runtime.InteropServices;
*/
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class AuthConfiguration : IAuthConfiguration
    {
        public bool EnableNegotiate => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && KeyAuthenticationMode != KeyAuthenticationMode.NoAuth;

        public KeyAuthenticationMode KeyAuthenticationMode { get; }

        public bool EnableKeyAuth => (KeyAuthenticationMode == KeyAuthenticationMode.StoredKey) ||
                                     (KeyAuthenticationMode == KeyAuthenticationMode.TemporaryKey);

        public GeneratedJwtKey TemporaryJwtKey { get; }

        public AuthConfiguration(KeyAuthenticationMode mode)
        {
            KeyAuthenticationMode = mode;

            if (mode == KeyAuthenticationMode.TemporaryKey)
            {
                TemporaryJwtKey = GeneratedJwtKey.Create();
            }
        }
    }
}
