// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
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
                    var children = providerOptionsSection.GetChildren();

                    if (options is ExtensionEgressProviderOptions eepOptions)
                    {
                        foreach (var child in children)
                        {
                            if (child.Value != null)
                            {
                                eepOptions.Add(child.Key, child.Value);
                            }
                            else
                            {
                                eepOptions.Add(child.Key, child.AsEnumerable().ToDictionary(k => k.Key, v => v.Value));
                            }
                        }
                    }

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
