// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    internal sealed class CollectionRuleTriggerOptionsProvider :
        ICollectionRuleTriggerOptionsProvider
    {
        private readonly IDictionary<string, Type> _optionsMap =
            new Dictionary<string, Type>(StringComparer.Ordinal);

        public CollectionRuleTriggerOptionsProvider(
            ILogger<CollectionRuleTriggerOptionsProvider> logger,
            IEnumerable<ICollectionRuleTriggerDescriptor> providers)
        {
            foreach (ICollectionRuleTriggerDescriptor provider in providers)
            {
                if (_optionsMap.ContainsKey(provider.TriggerName))
                {
                    logger.DuplicateCollectionRuleTriggerIgnored(provider.TriggerName);
                }
                else
                {
                    _optionsMap.Add(provider.TriggerName, provider.OptionsType);
                }
            }
        }

        public bool TryGetOptionsType(string triggerName, out Type optionsType)
        {
            return _optionsMap.TryGetValue(triggerName, out optionsType);
        }
    }
}
