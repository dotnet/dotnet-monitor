// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class InProcessFeaturesPostConfigureOptions :
        IPostConfigureOptions<InProcessFeaturesOptions>
    {
        private readonly IExperimentalFlags _experimentalFlags;

        public InProcessFeaturesPostConfigureOptions(IExperimentalFlags experimentalFlags)
        {
            _experimentalFlags = experimentalFlags;
        }

        public void PostConfigure(string name, InProcessFeaturesOptions options)
        {
            if (!_experimentalFlags.IsCallStacksEnabled &&
                !_experimentalFlags.IsExceptionsEnabled &&
                !_experimentalFlags.IsParameterCapturingEnabled)
            {
                options.Enabled = false;
            }
        }
    }
}
