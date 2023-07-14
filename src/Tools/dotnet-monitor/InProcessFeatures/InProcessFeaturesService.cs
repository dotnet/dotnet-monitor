// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.HostingStartup
{
    internal sealed class InProcessFeaturesService
    {
        private readonly ILogger<InProcessFeaturesService> _logger;
        private readonly ParameterCapturingOptions _parameterCapturingOptions;

        public InProcessFeaturesService(
            IOptions<ParameterCapturingOptions> parameterCapturingOptions,
            ILogger<InProcessFeaturesService> logger)
        {
            _parameterCapturingOptions = parameterCapturingOptions.Value;
            _logger = logger;
        }

        public async Task ApplyInProcessFeatures(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            try
            {
                DiagnosticsClient client = new DiagnosticsClient(endpointInfo.Endpoint);
                if (_parameterCapturingOptions.GetEnabled())
                {
                    await client.SetEnvironmentVariableAsync(
                        InProcessFeaturesIdentifiers.EnvironmentVariables.EnableParameterCapturing,
                        ToolIdentifiers.EnvVarEnabledValue,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.UnableToApplyInProcessFeatureFlags(ex);
            }
        }
    }
}
