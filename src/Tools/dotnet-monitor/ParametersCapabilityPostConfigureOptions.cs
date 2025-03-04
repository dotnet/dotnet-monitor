// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.Monitoring
{
    public class ParametersCapabilityPostConfigureOptions : CapabilityPostConfigureOptions<ParameterCapturingOptions>
    {
        public ParametersCapabilityPostConfigureOptions(IEnumerable<MonitorCapability> capabilities)
            : base(capabilities.First(c => c.Name == MonitorCapabilityConstants.ParameterCapturing))
        {
        }

        public override void PostConfigure(string? _, ParameterCapturingOptions options)
        {
            PostConfigure(options.Enabled ?? false);
        }
    }
}
