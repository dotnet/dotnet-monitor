// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration
{
    /// <summary>
    /// Provides access to the Egress:{ProviderType} section of the configuration that is
    /// associated with the <typeparamref name="TOptions"/> type.
    /// </summary>
    internal sealed class EgressProviderConfigurationProvider<TOptions> :
        IEgressProviderConfigurationProvider<TOptions>
    {
        public EgressProviderConfigurationProvider(IConfiguration configuration, string providerCategoryName)
        {
            ProviderCategory = providerCategoryName;
            Configuration = configuration.GetEgressSection().GetSection(providerCategoryName);
        }

        /// <inheritdoc/>
        public IConfiguration Configuration { get; }

        /// <inheritdoc/>
        public string ProviderCategory { get; }

        /// <inheritdoc/>
        public Type OptionsType => typeof(TOptions);
    }
}
