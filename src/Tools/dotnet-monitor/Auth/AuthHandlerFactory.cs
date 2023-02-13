﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey.Stored;
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

    internal static class AuthHandlerFactory
    {
        public static IAuthHandler Create(StartupAuthenticationMode startupAuthMode, HostBuilderContext context)
        {
            switch (startupAuthMode)
            {
                case StartupAuthenticationMode.NoAuth:
                    return new NoAuthHandler();

                case StartupAuthenticationMode.TemporaryKey:
                    return new MonitorTempKeyAuthHandler();

                case StartupAuthenticationMode.Deferred:
                    // We currently only have one configuration-based authentication mode.
                    return new MonitorKeyAuthHandler();

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
