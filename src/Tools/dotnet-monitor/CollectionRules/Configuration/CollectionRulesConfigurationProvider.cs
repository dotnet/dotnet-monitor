// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    internal class CollectionRulesConfigurationProvider
    {
        private readonly IConfigurationSection _section;

        public CollectionRulesConfigurationProvider(IConfiguration configuration)
        {
            _section = configuration.GetSection(nameof(ConfigurationKeys.CollectionRules));
        }

        /// <summary>
        /// Gets the list of configured collection rule names.
        /// </summary>
        public IReadOnlyCollection<string> GetCollectionRuleNames()
        {
            List<string> names = new();

            foreach (IConfigurationSection ruleSection in _section.GetChildren())
            {
                names.Add(ruleSection.Key);
            }

            return names.AsReadOnly();
        }

        /// <summary>
        /// Gets the configuration section associated with the specified collection rule name.
        /// </summary>
        public IConfigurationSection GetCollectionRuleSection(string name)
        {
            IConfigurationSection ruleSection = _section.GetSection(name);
            Debug.Assert(ruleSection.Exists());
            return ruleSection;
        }
    }
}
