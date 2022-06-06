﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    internal sealed class CollectionRuleConfigureNamedOptions :
        IConfigureNamedOptions<CollectionRuleOptions>
    {
        private readonly CollectionRulesConfigurationProvider _configurationProvider;

        public CollectionRuleConfigureNamedOptions(
            CollectionRulesConfigurationProvider configurationProvider)
        {
            _configurationProvider = configurationProvider;
        }

        public void Configure(string name, CollectionRuleOptions options)
        {
            IConfigurationSection ruleSection = _configurationProvider.GetCollectionRuleSection(name);
            if (ruleSection.Exists())
            {
                ruleSection.Bind(options);
            }
        }

        public void Configure(CollectionRuleOptions options)
        {
            throw new NotSupportedException();
        }
    }
}
