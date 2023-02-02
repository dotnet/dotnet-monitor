// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Diagnostics.Monitoring;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class AddressListenResultsStartupFilter :
        IStartupFilter
    {
        private readonly AddressListenResults _listenResults;

        public AddressListenResultsStartupFilter(AddressListenResults listenResults)
        {
            _listenResults = listenResults;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            // If we end up not listening on any ports, Kestrel defaults to port 5000. Make sure we don't attempt this.
            // Startup filters are called before KestrelServer is started
            // by the GenericWebHostServer, so there is no duplication of logging errors
            // and Kestrel does not bind to default ports.
            if (!_listenResults.AnyAddresses)
            {
                // This is logged by GenericWebHostServer.StartAsync
                throw new MonitoringException(Strings.ErrorMessage_UnableToBindUrls);
            }

            return next;
        }
    }
}
