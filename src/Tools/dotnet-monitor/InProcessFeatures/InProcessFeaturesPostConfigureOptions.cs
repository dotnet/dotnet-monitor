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
            // Stacks is currently the only in-process feature; if this feature is not
            // enabled, disable all in-process feature support.
            if (!_experimentalFlags.IsCallStacksEnabled && !_experimentalFlags.IsExceptionsEnabled)
            {
                options.Enabled = false;
            }
        }
    }
}
