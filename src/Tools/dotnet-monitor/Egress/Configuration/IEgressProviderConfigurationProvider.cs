// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration
{
    /// <summary>
    /// Provides access to all of the egress provider configuration blocks that use the given <see cref="OptionsType"/>.
    /// These configuration blocks will be from "Egress:{ProviderType}" where "{ProviderType}" is one of the strings defined in <see cref="ProviderTypes"/>.
    /// </summary>
    internal interface IEgressProviderConfigurationProvider
    {
        /// <summary>
        /// The name of the provider types defined in configuration that use this options type. These keys can be passed to <see cref="GetConfigurationSection(string)"/> to get the specific configuration section.
        /// </summary>
        IEnumerable<string> ProviderTypes { get; }

        /// <summary>
        /// The type of the options class associated with the egress providers. This type is intended to be 1-to-1 with the instance of <see cref="IEgressPropertiesConfigurationProvider"/>.
        /// </summary>
        Type OptionsType { get; }

        /// <summary>
        /// Gets the <see cref="IConfigurationSection"/> associated with the given <paramref name="providerType"/>.
        /// </summary>
        /// <param name="providerType">The provider type, the element under "Egress:" in configuration. You can use <see cref="ProviderTypes"/> to get valid strings to use here.</param>
        IConfigurationSection GetConfigurationSection(string providerType);

        /// <summary>
        /// The configuration section that should be monitored for changes. This will return the minimum set of <see cref="IConfigurationSection"/> to monitor for changes.
        /// Note: a change in this section does not guarantee a meaningful change to configuration.
        /// </summary>
        IConfigurationSection GetTokenChangeSourceSection();
    }

    /// <summary>
    /// Provides access to all of the egress provider configuration blocks that use the given <see cref="OptionsType"/>.
    /// These configuration blocks will be from "Egress:{ProviderType}" where "{ProviderType}" is one of the strings defined in <see cref="ProviderTypes"/>.
    /// This is the typed instance of <see cref="IEgressPropertiesConfigurationProvider"/>.
    /// </summary>
    internal interface IEgressProviderConfigurationProvider<TOptions> :
        IEgressProviderConfigurationProvider
    {
    }
}
