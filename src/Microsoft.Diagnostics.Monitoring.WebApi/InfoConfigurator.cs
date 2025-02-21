// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.Monitoring.Options
{
    public class InfoConfigurator
    {
        private readonly IEnumerable<IMonitorCapability> _monitorCapabilities;
        private readonly IEgressOutputConfiguration _egressOutputConfiguration;

        public InfoConfigurator(IEnumerable<IMonitorCapability> monitorCapabilities, IEgressOutputConfiguration egressOutputConfiguration)
        {
            _monitorCapabilities = monitorCapabilities;
            _egressOutputConfiguration = egressOutputConfiguration;
        }
        public List<IMonitorCapability> GetFeatureAvailability()
        {
            // Access the egress output configuration to trigger PostConfigure
            bool _ = _egressOutputConfiguration.IsHttpEgressEnabled;

            return _monitorCapabilities.ToList();
        }
    }
}
