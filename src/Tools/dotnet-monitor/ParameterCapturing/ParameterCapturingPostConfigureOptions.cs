// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing
{
    internal sealed class ParameterCapturingPostConfigureOptions :
        InProcessFeaturePostConfigureOptions<ParameterCapturingOptions>
    {
        public ParameterCapturingPostConfigureOptions(IConfiguration configuration)
            : base(configuration, ConfigurationKeys.InProcessFeatures_ParameterCapturing, ParameterCapturingOptionsDefaults.Enabled)
        {
        }
    }
}
