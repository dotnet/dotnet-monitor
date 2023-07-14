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
        private readonly IInProcessFeatures _inProcessFeatures;

        public ExperimentalStartupLogger(ILogger<Startup> logger, IInProcessFeatures inProcessFeatures)
        {
            _logger = logger;
            _inProcessFeatures = inProcessFeatures;
        }

        public void Log()
        {
            if (_inProcessFeatures.IsParameterCapturingEnabled)
            {
                _logger.ExperimentalFeatureEnabled(Strings.FeatureName_ParameterCapturing);
            }

            // Experimental features should log a warning when they are activated e.g.
            // _logger.ExperimentalFeatureEnabled("CallStacks");

        }
    }
}
