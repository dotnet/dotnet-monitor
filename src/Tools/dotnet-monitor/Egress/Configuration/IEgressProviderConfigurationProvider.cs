﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration
{
    /// <summary>
    /// Provides access to an Egress:{ProviderType} section of the configuration.
    /// </summary>
    internal interface IEgressProviderConfigurationProvider
    {
        /// <summary>
        /// The configuration section associated with the egress provider.
        /// </summary>
        IConfiguration Configuration { get; }

        /// <summary>
        /// The type of the egress provider.
        /// </summary>
        string ProviderType { get; }

        /// <summary>
        /// The type of the options class associated with the egress provider.
        /// </summary>
        Type OptionsType { get; }
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
