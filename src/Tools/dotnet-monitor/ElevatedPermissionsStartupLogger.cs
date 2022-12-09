// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class ElevatedPermissionsStartupLogger :
        IStartupLogger
    {
        private readonly IAuthConfiguration _authConfiguration;
        private readonly ILogger _logger;

        public ElevatedPermissionsStartupLogger(
            IAuthConfiguration authConfiguration,
            ILogger<Startup> logger)
        {
            _authConfiguration = authConfiguration;
            _logger = logger;
        }

        public void Log()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(currentUser);
                if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                {
                    _logger.RunningElevated();
                    // In the future this will need to be modified when ephemeral keys are setup
                    if (_authConfiguration.EnableNegotiate)
                    {
                        _logger.DisabledNegotiateWhileElevated();
                    }
                }
            }

            // in the future we should check that we aren't running root on linux (out of scope for now)
        }
    }
}
