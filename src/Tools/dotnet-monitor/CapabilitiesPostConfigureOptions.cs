// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Monitoring
{
    public abstract class CapabilityPostConfigureOptions<T> : IPostConfigureOptions<T> where T : class
    {
        private MonitorCapability? _capability;

        public CapabilityPostConfigureOptions(MonitorCapability? capability)
        {
            _capability = capability;
        }

        public abstract void PostConfigure(string? name, T options);

        public void PostConfigure(bool isEnabled)
        {
            if (_capability != null)
            {
                _capability.Enabled = isEnabled;
            }
        }
    }
}
