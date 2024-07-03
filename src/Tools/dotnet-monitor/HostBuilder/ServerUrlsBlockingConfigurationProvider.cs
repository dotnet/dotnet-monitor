// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class ServerUrlsBlockingConfigurationProvider :
        ConfigurationProvider
    {
        private readonly ServerUrlsBlockingConfigurationManager _manager;

        public ServerUrlsBlockingConfigurationProvider(ServerUrlsBlockingConfigurationManager manager)
        {
            _manager = manager;
        }

        public override void Set(string key, string? value)
        {
            // Overridden to prevent set of data since this provider does not
            // provide any real data from any source.
        }

        public override bool TryGet(string key, [NotNullWhen(true)] out string? value)
        {
            // Block reading of the Urls option if the manager says to block. This will prevent other
            // configuration providers that were configured at a lower priority from providing their
            // value of the Urls option to configuration.
            if (_manager.IsBlocking && WebHostDefaults.ServerUrlsKey.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                value = string.Empty;
                return true;
            }
            return base.TryGet(key, out value);
        }
    }
}
