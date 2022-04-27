// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration
{
    /// <summary>
    /// Provides access to a set of Egress:{ProviderType} sections of the configuration based on the type of options.
    /// </summary>
    internal interface IEgressProviderConfigurationProvider
    {
        /// <summary>
        /// The name of the category defined in configuration of this provider.
        /// </summary>
        IEnumerable<string> ProviderTypes { get; }

        /// <summary>
        /// The type of the options class associated with the egress provider.
        /// </summary>
        Type OptionsType { get; }

        /// <summary>
        /// The configuration section associated with the egress provider.
        /// </summary>
        IConfigurationSection GetConfigurationSection(string providerType);

        /// <summary>
        /// The configuration section that should be monitored for changes.
        /// </summary>
        IConfigurationSection GetTokenChangeSourceSection();
    }

    /// <summary>
    /// Provides access to the Egress:{ProviderType} section of the configuration that is
    /// associated with the <typeparamref name="TOptions"/> type.
    /// </summary>
    internal interface IEgressProviderConfigurationProvider<TOptions> :
        IEgressProviderConfigurationProvider
    {
    }
}
