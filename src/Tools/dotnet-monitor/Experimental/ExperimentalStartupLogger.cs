// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class ExperimentalStartupLogger :
        IStartupLogger
    {
        private readonly ILogger _logger;
        private readonly IExperimentalFlags _experimentalFlags;

        public ExperimentalStartupLogger(ILogger<Startup> logger, IExperimentalFlags experimentalFlags)
        {
            _logger = logger;
            _experimentalFlags = experimentalFlags;
        }

        public void Log()
        {
            // Experimental features should log a warning when they are activated e.g.
            // _logger.ExperimentalFeatureEnabled("CallStacks");
        }
    }
}
