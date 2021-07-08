// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal sealed class EgressOutputOptions : IEgressOutputOptions
    {
        /*
        public bool EnableNegotiate => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && KeyAuthenticationMode != KeyAuthenticationMode.NoAuth;

        public KeyAuthenticationMode KeyAuthenticationMode { get; }

        public bool EnableKeyAuth => (KeyAuthenticationMode == KeyAuthenticationMode.StoredKey) ||
                                     (KeyAuthenticationMode == KeyAuthenticationMode.TemporaryKey);

        public GeneratedApiKey TemporaryKey { get; }
        */

        public EgressMode EgressMode { get; }

        public EgressOutputOptions(EgressMode mode)
        {
            EgressMode = mode;

            /*
            if (mode == EgressMode.HTTPDisabled)
            {
                TemporaryKey = GeneratedApiKey.Create();
            }
            */
        }
    }
}
