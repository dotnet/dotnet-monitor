// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration
{
    /// <summary>
    /// Provides access to the Egress:{ProviderType} section of the configuration that is
    /// associated with the <typeparamref name="TOptions"/> type.
    /// </summary>
    internal sealed class EgressProviderConfigurationProvider<TOptions> :
        IEgressProviderConfigurationProvider<TOptions>
    {
        private readonly IOptionsTypeToProviderTypesMapper _optionsMapper;
        private readonly IConfigurationSection _egressSection;

        public EgressProviderConfigurationProvider(IConfiguration configuration, IOptionsTypeToProviderTypesMapper optionsMapper)
        {
            _optionsMapper  = optionsMapper;
            _egressSection = configuration.GetEgressSection();
        }

        /// <inheritdoc/>
        public Type OptionsType => typeof(TOptions);

        /// <inheritdoc/>
        public IEnumerable<string> ProviderTypes
        {
            get
            {
                return _optionsMapper.GetProviderSections(this.OptionsType).Select(s => s.Key);
            }
        }

        /// <inheritdoc/>
        public IConfigurationSection GetConfigurationSection(string providerType)
        {
            return _optionsMapper.GetProviderSections(this.OptionsType).First(s => s.Key == providerType);
        }

        /// <inheritdoc/>
        public IConfigurationSection GetTokenChangeSourceSection()
        {
            IConfigurationSection[] sections = _optionsMapper.GetProviderSections(this.OptionsType).ToArray();

            // If we are monitoring a specific section, return that element, otherwise we need to monitor the whole egress section
            if (sections.Length == 1)
            {
                return sections[0];
            }

            return _egressSection;
        }
    }
}
