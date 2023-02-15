// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class ElevatedPermissionsStartupLogger :
        IStartupLogger
    {
        private readonly ILogger _logger;

        public ElevatedPermissionsStartupLogger(
            ILogger<Startup> logger)
        {
            _logger = logger;
        }

        public void Log()
        {
            if (EnvironmentInformation.IsElevated)
            {
                _logger.RunningElevated();
            }
        }
    }
}
