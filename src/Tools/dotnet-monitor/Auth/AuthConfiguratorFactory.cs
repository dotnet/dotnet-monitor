// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey;
using Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey.Temporary;
using Microsoft.Diagnostics.Tools.Monitor.Auth.NoAuth;
using Microsoft.Extensions.Hosting;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth
{
    internal enum StartupAuthenticationMode
    {
        NoAuth,
        TemporaryKey,
        Deferred
    }

    internal static class AuthConfiguratorFactory
    {
        public static IAuthenticationConfigurator Create(StartupAuthenticationMode startupAuthMode, HostBuilderContext context)
        {
            switch (startupAuthMode)
            {
                case StartupAuthenticationMode.NoAuth:
                    return new NoAuthConfigurator();

                case StartupAuthenticationMode.TemporaryKey:
                    if (context.Properties.TryGetValue(typeof(GeneratedJwtKey), out object generatedJwtKeyObject))
                    {
                        if (generatedJwtKeyObject is GeneratedJwtKey generatedJwtKey)
                        {
                            return new MonitorKeyAuthConfigurator(generatedJwtKey);
                        }
                    }

                    // We should never reach here unless there is a bug in our initialization code.
                    throw new InvalidOperationException("GeneratedJwtKey was not found.");

                case StartupAuthenticationMode.Deferred:
                    // We currently only have one configuration-based authentication mode.
                    return new MonitorKeyAuthConfigurator();

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
