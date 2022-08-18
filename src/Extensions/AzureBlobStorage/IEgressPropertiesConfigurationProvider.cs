// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration
{
    /// <summary>
    /// Provides access to the Egress:Properties section of the configuration.
    /// </summary>
    internal interface IEgressPropertiesConfigurationProvider
    {
        /// <summary>
        /// The configuration section associated with the egress properties.
        /// </summary>
        IConfiguration Configuration { get; }
    }
}
