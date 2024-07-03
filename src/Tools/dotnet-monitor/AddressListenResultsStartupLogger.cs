// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class AddressListenResultsStartupLogger :
        IStartupLogger,
        IDisposable
    {
        private readonly CancellationTokenRegistration _applicationStartedRegistration;
        private readonly AddressListenResults _listenResults;
        private readonly ILogger _logger;
        private readonly IServer _server;

        public AddressListenResultsStartupLogger(
            AddressListenResults listenResults,
            IHostApplicationLifetime lifetime,
            IServer server,
            ILogger<Startup> logger)
        {
            _listenResults = listenResults;
            _logger = logger;
            _server = server;

#nullable disable
            _applicationStartedRegistration = lifetime.ApplicationStarted.Register(
                l => ((AddressListenResultsStartupLogger)l).OnStarted(),
                this);
#nullable restore
        }

        public void Dispose()
        {
            _applicationStartedRegistration.Dispose();
        }

        public void Log()
        {
            foreach (AddressListenResult result in _listenResults.Errors)
            {
                _logger.UnableToListenToAddress(result.Url, result.Exception);
            }

            if (_listenResults.HasInsecureAuthentication)
            {
                _logger.InsecureAuthenticationConfiguration();
            }
        }

#nullable disable
        private void OnStarted()
        {
            IServerAddressesFeature serverAddresses = _server.Features.Get<IServerAddressesFeature>();

            // This logging allows the tool to differentiate which addresses
            // are default address and which are metrics addresses.

            foreach (string defaultAddress in _listenResults.GetDefaultAddresses(serverAddresses))
            {
                _logger.BoundDefaultAddress(defaultAddress);
            }

            foreach (string metricAddress in _listenResults.GetMetricsAddresses(serverAddresses))
            {
                _logger.BoundMetricsAddress(metricAddress);
            }
        }
#nullable restore
    }
}
