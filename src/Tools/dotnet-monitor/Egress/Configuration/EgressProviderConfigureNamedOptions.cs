// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;

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
        private readonly IEgressProviderConfigurationProvider<TOptions> _provider;

        public EgressProviderConfigureNamedOptions(IEgressProviderConfigurationProvider<TOptions> provider)
        {
            _provider = provider;
        }

        public void Configure(string name, TOptions options)
        {
            foreach (string providerType in _provider.ProviderTypes)
            {
                IConfigurationSection providerTypeSection = _provider.GetConfigurationSection(providerType);
                IConfigurationSection providerOptionsSection = providerTypeSection.GetSection(name);
                if (providerOptionsSection.Exists())
                {
                    providerOptionsSection.Bind(options);
                    return;
                }
            }

            throw new EgressException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressProviderDoesNotExist, name));
        }

        public void Configure(TOptions options)
        {
            throw new NotSupportedException();
        }
    }
}
