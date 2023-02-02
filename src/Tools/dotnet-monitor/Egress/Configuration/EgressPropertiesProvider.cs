// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration
{
    /// <summary>
    /// Provides strongly-typed access to the values described in the Egress:Properties section.
    /// </summary>
    internal sealed class EgressPropertiesProvider :
        IEgressPropertiesProvider
    {
        private readonly IEgressPropertiesConfigurationProvider _provider;

        public EgressPropertiesProvider(IEgressPropertiesConfigurationProvider provider)
        {
            _provider = provider;
        }

        /// <inheritdoc/>
        public IDictionary<string, string> GetAllProperties()
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            foreach (IConfigurationSection section in _provider.Configuration.GetChildren())
            {
                properties.Add(section.Key, section.Value);
            }
            return properties;
        }

        /// <inheritdoc/>
        public bool TryGetPropertyValue(string key, out string value)
        {
            IConfigurationSection section = _provider.Configuration.GetSection(key);
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
