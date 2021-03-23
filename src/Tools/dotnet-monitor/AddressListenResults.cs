// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    
    // Consolidates the configuration of addresses in Kestrel and recording which address
    // is which type (default vs metrics). The recording logic uses the count of each type
    // of correctly configured address so it makes sense to keep this logic within the same
    // class so that the details are not leaked into other abstractions.
    internal sealed class AddressListenResults
    {
        private int _defaultAddressCount;
        private int _metricsAddressCount;

        public IList<AddressListenResult> Errors { get; }
            = new List<AddressListenResult>();

        //CONSIDER Should these be more structured?
        public IList<string> Warnings { get; } = new List<string>();

        public bool AnyAddresses => (_defaultAddressCount + _metricsAddressCount) > 0;

        public IEnumerable<string> GetDefaultAddresses(IServerAddressesFeature serverAddresses)
        {
            Debug.Assert(serverAddresses.Addresses.Count == _defaultAddressCount + _metricsAddressCount);
            return serverAddresses.Addresses.Take(_defaultAddressCount);
        }

        public IEnumerable<string> GetMetricsAddresses(IServerAddressesFeature serverAddresses)
        {
            Debug.Assert(serverAddresses.Addresses.Count == _defaultAddressCount + _metricsAddressCount);
            return serverAddresses.Addresses.Skip(_defaultAddressCount);
        }

        /// <summary>
        /// Configures <see cref="KestrelServerOptions"/> with the specified default and metrics URLs.
        /// </summary>
        public void Listen(KestrelServerOptions options, IEnumerable<string> defaultUrls, IEnumerable<string> metricsUrls)
        {
            foreach (string url in defaultUrls)
            {
                if (Listen(options, url))
                {
                    _defaultAddressCount++;
                }
            }

            foreach (string url in metricsUrls)
            {
                if (Listen(options, url))
                {
                    _metricsAddressCount++;
                }
            }
        }

        /// <summary>
        /// Configure the <see cref="KestrelServerOptions"/> with the specified URL.
        /// </summary>
        private bool Listen(KestrelServerOptions options, string url)
        {
            BindingAddress address = null;
            try
            {
                address = BindingAddress.Parse(url);
            }
            catch (Exception ex)
            {
                // Record the exception; it will be logged later through ILogger.
                Errors.Add(new AddressListenResult(url, ex));
                return false;
            }

            Action<ListenOptions> configureListenOptions = (listenOptions) =>
            {
                if (address.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                {
                    listenOptions.UseHttps();
                }
            };

            try
            {
                if (address.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    options.ListenLocalhost(address.Port, configureListenOptions);
                }
                else if (IPAddress.TryParse(address.Host, out IPAddress ipAddress))
                {
                    options.Listen(ipAddress, address.Port, configureListenOptions);
                }
                else
                {
                    options.ListenAnyIP(address.Port, configureListenOptions);
                }
            }
            catch (InvalidOperationException ex)
            {
                // This binding failure is typically due to missing default certificate.
                // Record the exception; it will be logged later through ILogger.
                Errors.Add(new AddressListenResult(url, ex));
                return false;
            }

            return true;
        }
    }

    internal sealed class AddressListenResult
    {
        public readonly string Url;

        public readonly Exception Exception;

        public AddressListenResult(string Url, Exception exception)
        {
            this.Url = Url;
            Exception = exception;
        }
    }
}
