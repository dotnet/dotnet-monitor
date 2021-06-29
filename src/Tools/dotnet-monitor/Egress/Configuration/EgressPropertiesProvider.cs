// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration
{
    /// <summary>
    /// Provides strongly-typed access to the values described in the Egress:Properties section.
    /// </summary>
    internal sealed class EgressPropertiesProvider :
        IEgressPropertiesProvider
    {
        private readonly IEgressPropertiesConfigurationProvider _configuration;

        public EgressPropertiesProvider(IEgressPropertiesConfigurationProvider configuration)
        {
            _configuration = configuration;
        }

        /// <inheritdoc/>
        public bool TryGetPropertyValue(string key, out string value)
        {
            IConfigurationSection section = _configuration.Configuration.GetSection(key);
            if (!section.Exists())
            {
                value = null;
                return false;
            }
            value = section.Value;
            return true;
        }
    }
}
