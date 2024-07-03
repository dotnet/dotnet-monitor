// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    internal class CollectionRulesConfigurationProvider : IDisposable
    {
        private readonly IDisposable _changeRegistration;
        private readonly IConfigurationSection _section;

        public CollectionRulesConfigurationProvider(IConfiguration configuration)
        {
            _section = configuration.GetSection(nameof(ConfigurationKeys.CollectionRules));

            _changeRegistration = ChangeToken.OnChange(
                () => _section.GetReloadToken(),
                () => RulesChanged?.Invoke(this, EventArgs.Empty));
        }

        public void Dispose()
        {
            _changeRegistration.Dispose();
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

        public IConfigurationSection Configuration => _section;

        public event EventHandler? RulesChanged;
    }
}
