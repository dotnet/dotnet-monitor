// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration
{
    internal static class EgressConfigurationExtensions
    {
        /// <summary>
        /// Get the Egress configuration section from the specified configuration.
        /// </summary>
        public static IConfigurationSection GetEgressSection(this IConfiguration configuration)
        {
            return configuration.GetSection(ConfigurationKeys.Egress);
        }

        /// <summary>
        /// Get the Egress:Properties configuration section from the specified configuration.
        /// </summary>
        public static IConfigurationSection GetEgressPropertiesSection(this IConfiguration configuration)
        {
            return configuration.GetEgressSection().GetSection(ConfigurationKeys.Egress_Properties);
        }
    }
}
