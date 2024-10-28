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
        private readonly ParameterCapturingOptions _parameterCapturingOptions;
        private readonly ServerEndpointTrackerV2? _serverEndpointTrackerV2;

        public ExperimentalStartupLogger(
            ILogger<Startup> logger,
            IOptions<ParameterCapturingOptions> parameterCapturingOptions,
            ServerEndpointTrackerV2? serverEndpointTrackerV2 = null)
        {
            _logger = logger;
            _parameterCapturingOptions = parameterCapturingOptions.Value;
            _serverEndpointTrackerV2 = serverEndpointTrackerV2;
        }

        public void Log()
        {
            if (_parameterCapturingOptions.GetEnabled())
            {
                _logger.ExperimentalFeatureEnabled(Microsoft.Diagnostics.Monitoring.WebApi.Strings.FeatureName_ParameterCapturing);
            }

            if (_serverEndpointTrackerV2 != null)
            {
                _logger.ExperimentalFeatureEnabled(Microsoft.Diagnostics.Monitoring.WebApi.Strings.FeatureName_ServerEndpointPruningAlgorithmV2);
            }

            // Experimental features should log a warning when they are activated e.g.
            // _logger.ExperimentalFeatureEnabled("CallStacks");

        }
    }
}
