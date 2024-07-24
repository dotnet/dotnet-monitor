// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal class MetricsPortsProvider : IMetricsPortsProvider
    {
        private readonly AddressListenResults _results;
#nullable disable
        private readonly IServerAddressesFeature _serverAddresses;
#nullable restore

        public MetricsPortsProvider(AddressListenResults results, IServer server)
        {
            _results = results;
            _serverAddresses = server.Features.Get<IServerAddressesFeature>();
        }

        public IEnumerable<int> MetricsPorts
        {
            get
            {
                IList<int> metricsPorts = new List<int>(_serverAddresses.Addresses.Count);
                foreach (string metricsAddress in _results.GetMetricsAddresses(_serverAddresses))
                {
                    try
                    {
                        // BindingAddress is only available in .NET Core 3.0+
                        metricsPorts.Add(BindingAddress.Parse(metricsAddress).Port);
                    }
                    catch (Exception ex)
                    {
                        // At this point, expect to parse the addresses since these should be
                        // the ones bound by Kestrel.
                        Debug.Fail("Unable to parse address.", ex.Message);
                    }
                }
                return metricsPorts;
            }
        }
    }
}
