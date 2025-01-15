// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Options;
namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class InProcessFeatures : IInProcessFeatures
    {
        private readonly CallStacksOptions _callStacksOptions;
        private readonly ExceptionsOptions _exceptionsOptions;
        private readonly ParameterCapturingOptions _parameterCapturingOptions;

        public InProcessFeatures(
            IOptions<CallStacksOptions> callStacksOptions,
            IOptions<ExceptionsOptions> exceptionsOptions,
            IOptions<ParameterCapturingOptions> parameterCapturingOptions)
        {
            _callStacksOptions = callStacksOptions.Value;
            _exceptionsOptions = exceptionsOptions.Value;
            _parameterCapturingOptions = parameterCapturingOptions.Value;
        }

        private bool IsCallStacksEnabled => _callStacksOptions.GetEnabled();

        private bool IsExceptionsEnabled => _exceptionsOptions.GetEnabled();

        private bool IsParameterCapturingEnabled => _parameterCapturingOptions.GetEnabled();

        public bool IsProfilerRequired => IsCallStacksEnabled || IsParameterCapturingEnabled;

        public bool IsMutatingProfilerRequired => IsParameterCapturingEnabled;

        public bool IsStartupHookRequired => IsParameterCapturingEnabled || IsExceptionsEnabled;

        public bool IsLibrarySharingRequired => IsProfilerRequired || IsStartupHookRequired;
    }
}
