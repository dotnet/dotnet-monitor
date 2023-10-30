// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration
{
    /// <summary>
    /// Provides access to all of the egress provider configuration blocks.
    /// These configuration blocks will be from "Egress:{ProviderType}" where "{ProviderType}" is one of the strings defined in <see cref="ProviderTypes"/>.
    /// </summary>
    internal interface IEgressConfigurationProvider
    {
        /// <summary>
        /// The name of the provider types defined in configuration that use this options type. These keys can be passed to <see cref="GetProviderTypeConfigurationSection(string)"/> to get the specific configuration section.
        /// </summary>
        IEnumerable<string> ProviderTypes { get; }

        /// <summary>
        /// Gets the set of keys defined as a <see cref="IDictionary{TKey, TValue}"/> with values populated.
        /// </summary>
        /// <returns><see cref="IDictionary{TKey, TValue}"/> representing the set of properties.</returns>
        IDictionary<string, string> GetAllProperties();

        /// <summary>
        /// Gets the <see cref="IConfigurationSection"/> associated with the given <paramref name="providerType"/> and <paramref name="providerName"/>.
        /// </summary>
        /// <param name="providerType">The provider type, the element under "Egress:" in configuration. You can use <see cref="ProviderTypes"/> to get valid strings to use here.</param>
        /// <param name="providerName">The provider name, the element under "Egress:{providerType}:" in configuration.</param>
        IConfigurationSection GetProviderConfigurationSection(string providerType, string providerName);

        /// <summary>
        /// Gets the <see cref="IConfigurationSection"/> associated with the given <paramref name="providerType"/>.
        /// </summary>
        /// <param name="providerType">The provider type, the element under "Egress:" in configuration. You can use <see cref="ProviderTypes"/> to get valid strings to use here.</param>
        IConfigurationSection GetProviderTypeConfigurationSection(string providerType);

        IChangeToken GetReloadToken();
    }
}
