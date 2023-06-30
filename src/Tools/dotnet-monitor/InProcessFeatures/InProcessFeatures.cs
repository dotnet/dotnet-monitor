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

        public InProcessFeatures(IOptions<CallStacksOptions> callStacksOptions, IOptions<ExceptionsOptions> exceptionsOptions)
        {
            _callStacksOptions = callStacksOptions.Value;
            _exceptionsOptions = exceptionsOptions.Value;
        }

        private bool IsCallStacksEnabled => _callStacksOptions.GetEnabled();

        private bool IsExceptionsEnabled => _exceptionsOptions.GetEnabled();

        public bool IsProfilerRequired => IsCallStacksEnabled;

        public bool IsLibrarySharingRequired => IsCallStacksEnabled || IsExceptionsEnabled;
    }
}
