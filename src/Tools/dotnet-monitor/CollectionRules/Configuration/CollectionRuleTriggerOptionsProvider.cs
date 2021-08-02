// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    internal sealed class CollectionRuleTriggerOptionsProvider :
        ICollectionRuleTriggerOptionsProvider
    {
        private readonly IEnumerable<ICollectionRuleTriggerProvider> _triggerProviders;

        public CollectionRuleTriggerOptionsProvider(IEnumerable<ICollectionRuleTriggerProvider> triggerProviders)
        {
            _triggerProviders = triggerProviders;
        }

        public bool TryGetOptionsType(string triggerType, out Type optionsType)
        {
            ICollectionRuleTriggerProvider triggerProvider = _triggerProviders
                .FirstOrDefault(provider => string.Equals(triggerType, provider.TriggerType, StringComparison.Ordinal));
            if (null == triggerProvider)
            {
                optionsType = null;
                return false;
            }

            optionsType = triggerProvider.OptionsType;
            return true;
        }
    }
}
