// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class ExperimentalStartupLogger :
        IStartupLogger
    {
        private readonly ILogger _logger;
        private ParameterCapturingOptions _parameterCapturingOptions;

        public ExperimentalStartupLogger(ILogger<Startup> logger, IOptions<ParameterCapturingOptions> parameterCapturingOptions)
        {
            _logger = logger;
            _parameterCapturingOptions = parameterCapturingOptions.Value;
        }

        public void Log()
        {
            if (_parameterCapturingOptions.GetEnabled())
            {
                _logger.ExperimentalFeatureEnabled(Strings.FeatureName_ParameterCapturing);
            }

            // Experimental features should log a warning when they are activated e.g.
            // _logger.ExperimentalFeatureEnabled("CallStacks");

        }
    }
}
