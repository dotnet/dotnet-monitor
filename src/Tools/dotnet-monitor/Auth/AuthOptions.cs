// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class AuthOptions : IAuthOptions
    {
        public bool EnableNegotiate => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && KeyAuthenticationMode != KeyAuthenticationMode.NoAuth;

        public KeyAuthenticationMode KeyAuthenticationMode { get; }

        public bool EnableKeyAuth => (KeyAuthenticationMode == KeyAuthenticationMode.StoredKey) ||
                                     (KeyAuthenticationMode == KeyAuthenticationMode.TemporaryKey);
        
        public byte[] GeneratedKey { get; }

        public AuthOptions(KeyAuthenticationMode mode)
        {
            KeyAuthenticationMode = mode;
            
            if (mode == KeyAuthenticationMode.TemporaryKey)
            {
                byte[] newKey = new byte[32];
                RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
                rngCsp.GetBytes(newKey);
                GeneratedKey = newKey;
            }
        }
    }
}
