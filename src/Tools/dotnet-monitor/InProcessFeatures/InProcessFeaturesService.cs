// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.StartupHook
{
    internal sealed class InProcessFeaturesService
    {
        private readonly ILogger<InProcessFeaturesService> _logger;
        private readonly ParameterCapturingOptions _parameterCapturingOptions;
        private readonly ExceptionsOptions _exceptionOptions;
        private readonly DotnetMonitorDebugOptions _debugOptions;

        public InProcessFeaturesService(
            IOptions<ParameterCapturingOptions> parameterCapturingOptions,
            IOptions<ExceptionsOptions> exceptionOptions,
            IOptions<DotnetMonitorDebugOptions> debugOptions,
            ILogger<InProcessFeaturesService> logger)
        {
            _parameterCapturingOptions = parameterCapturingOptions.Value;
            _exceptionOptions = exceptionOptions.Value;
            _debugOptions = debugOptions.Value;
            _logger = logger;
        }

        public async Task ApplyInProcessFeatures(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            try
            {
                DiagnosticsClient client = new DiagnosticsClient(endpointInfo.Endpoint);
                // Exceptions
                if (_exceptionOptions.GetEnabled() && _debugOptions.Exceptions.GetIncludeMonitorExceptions())
                {
                    await client.SetEnvironmentVariableAsync(
                        InProcessFeaturesIdentifiers.EnvironmentVariables.Exceptions.IncludeMonitorExceptions,
                        ToolIdentifiers.EnvVarEnabledValue,
                        cancellationToken);
                }

                // Parameter Capturing
                if (_parameterCapturingOptions.GetEnabled() && CaptureParametersOperation.IsEndpointRuntimeSupported(endpointInfo))
                {
                    await client.SetEnvironmentVariableAsync(
                        InProcessFeaturesIdentifiers.EnvironmentVariables.ParameterCapturing.Enable,
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
