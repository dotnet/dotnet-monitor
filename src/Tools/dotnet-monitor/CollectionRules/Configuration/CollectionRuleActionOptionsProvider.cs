// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    internal sealed class CollectionRuleActionOptionsProvider :
        ICollectionRuleActionOptionsProvider
    {
        private readonly IDictionary<string, Type> _optionsMap =
            new Dictionary<string, Type>(StringComparer.Ordinal);

        public CollectionRuleActionOptionsProvider(
            ILogger<CollectionRuleActionOptionsProvider> logger,
            IEnumerable<ICollectionRuleActionDescriptor> providers)
        {
            foreach (ICollectionRuleActionDescriptor provider in providers)
            {
                if (_optionsMap.ContainsKey(provider.ActionName))
                {
                    logger.DuplicateCollectionRuleActionIgnored(provider.ActionName);
                }
                else
                {
                    _optionsMap.Add(provider.ActionName, provider.OptionsType);
                }
            }
        }

        public bool TryGetOptionsType(string actionName, out Type optionsType)
        {
            return _optionsMap.TryGetValue(actionName, out optionsType);
        }
    }
}
