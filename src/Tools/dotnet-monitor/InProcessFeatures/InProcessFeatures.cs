﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class InProcessFeatures : IInProcessFeatures
    {
        private readonly InProcessFeaturesOptions _options;
        private readonly IExperimentalFlags _experimentalFlags;

        public InProcessFeatures(IOptions<InProcessFeaturesOptions> options, IExperimentalFlags experimentalFlags)
        {
            _options = options.Value;
            _experimentalFlags = experimentalFlags;
        }

        public bool IsCallStacksEnabled => _options.GetEnabled() && _experimentalFlags.IsCallStacksEnabled;

        public bool IsExceptionsEnabled => _options.GetEnabled() && _experimentalFlags.IsExceptionsEnabled;

        public bool IsParameterCapturingEnabled => _options.GetEnabled() && _experimentalFlags.IsParameterCapturingEnabled;

        public bool IsProfilerRequired => IsCallStacksEnabled || IsParameterCapturingEnabled;

        public bool IsHostingStartupRequired => IsParameterCapturingEnabled;

        public bool IsLibrarySharingRequired => IsCallStacksEnabled || IsExceptionsEnabled || IsParameterCapturingEnabled;
    }
}
