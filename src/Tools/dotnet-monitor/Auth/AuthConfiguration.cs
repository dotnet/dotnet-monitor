// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
