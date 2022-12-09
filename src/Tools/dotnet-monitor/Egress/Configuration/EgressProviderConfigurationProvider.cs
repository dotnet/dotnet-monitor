// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public EgressProviderConfigurationProvider(IConfiguration configuration, string providerType)
        {
            ProviderType = providerType;
            Configuration = configuration.GetEgressSection().GetSection(providerType);
        }

        /// <inheritdoc/>
        public IConfiguration Configuration { get; }

        /// <inheritdoc/>
        public string ProviderType { get; }

        /// <inheritdoc/>
        public Type OptionsType => typeof(TOptions);
    }
}
