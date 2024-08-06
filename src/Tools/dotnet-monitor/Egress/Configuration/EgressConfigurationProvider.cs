// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration
{
    /// <summary>
    /// Provides access to the Egress:{ProviderType} sections of the configuration.
    /// </summary>
    internal sealed class EgressConfigurationProvider :
        IEgressConfigurationProvider
    {
        private readonly IConfigurationSection _egressSection;
        private readonly IConfigurationSection _propertiesSection;

        public EgressConfigurationProvider(IConfiguration configuration)
        {
            _egressSection = configuration.GetEgressSection();
            _propertiesSection = configuration.GetEgressPropertiesSection();
        }

        /// <inheritdoc/>
        public IEnumerable<string> ProviderTypes
        {
            get
            {
                return GetProviderSections().Select(s => s.Key);
            }
        }

        /// <inheritdoc/>
        public IDictionary<string, string> GetAllProperties()
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            foreach (IConfigurationSection section in _propertiesSection.GetChildren())
            {
                properties.Add(section.Key, section.Value);
            }
            return properties;
        }

        public IConfigurationSection GetProviderConfigurationSection(string providerType, string providerName)
        {
            IConfigurationSection section = GetProviderTypeConfigurationSection(providerType).GetSection(providerName);

            if (!section.Exists())
            {
                throw new EgressException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressProviderDoesNotExist, providerName));
            }

            return section;
        }

        /// <inheritdoc/>
        public IConfigurationSection GetProviderTypeConfigurationSection(string providerType)
        {
            foreach (IConfigurationSection providerTypeSection in GetProviderSections())
            {
                if (providerType == providerTypeSection.Key)
                {
                    return providerTypeSection;
                }
            }

            throw new EgressException(string.Format(CultureInfo.InvariantCulture, Strings.ErrorMessage_EgressProviderTypeNotRegistered, providerType));
        }

        public IChangeToken GetReloadToken()
        {
            return _egressSection.GetReloadToken();
        }

        /// <inheritdoc/>
        private IEnumerable<IConfigurationSection> GetProviderSections()
        {
            foreach (IConfigurationSection providerTypeSection in _egressSection.GetChildren())
            {
                if (providerTypeSection.Exists() && !IsBuiltInSection(providerTypeSection))
                {
                    yield return providerTypeSection;
                }
            }
        }

        /// <summary>
        /// Determines if the given section is a built in type property/field of the Egress configuration section.
        /// </summary>
        /// <param name="s">The <see cref="IConfigurationSection"/> to evaluate, this should be one of the children of the "Egress" section.</param>
        /// <returns><see langword="true"/> if and only if the given section is one of of the pre-defined</returns>
        private static bool IsBuiltInSection(IConfigurationSection s)
        {
            // The "Properties" key is the only built-in section under the "Egress" section.
            // All other child sections should be egress providers that are contributed via extensibility.
            return ConfigurationKeys.Egress_Properties == s.Key;
        }
    }
}
