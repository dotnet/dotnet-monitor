// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration
{
    /// <summary>
    /// Configure an <see cref="TOptions"/> by binding to
    /// its associated Egress:{ProviderType}:{Name} section in the configuration.
    /// </summary>
    /// <remarks>
    /// Only named options are support for egress providers.
    /// </remarks>
    internal sealed class EgressProviderConfigureNamedOptions<TOptions> :
        IConfigureNamedOptions<TOptions> where TOptions : class
    {
        private readonly IEnumerable<IEgressProviderConfigurationProvider> _providers;
        private readonly IEgressService _egressService;

        public EgressProviderConfigureNamedOptions(IEgressService egressService, IEnumerable<IEgressProviderConfigurationProvider> providers)
        {
            _egressService = egressService;
            _providers = providers;
        }

        public void Configure(string name, TOptions options)
        {
            string providerCategory = _egressService.GetProviderCategory(name);
            IEgressProviderConfigurationProvider provider = _providers.First(p => p.ProviderCategory == providerCategory);

            if (!(provider is IEgressProviderConfigurationProvider<TOptions>))
            {
                throw new InvalidOperationException();
            }

            IConfigurationSection section = provider.Configuration.GetSection(name);
            Debug.Assert(section.Exists());
            if (section.Exists())
            {
                section.Bind(options);
            }
        }

        public void Configure(TOptions options)
        {
            throw new NotSupportedException();
        }
    }
}
