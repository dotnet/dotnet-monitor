// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    internal sealed class CollectionRuleActionOptionsProvider :
        ICollectionRuleActionOptionsProvider
    {
        private readonly IEnumerable<ICollectionRuleActionProvider> _actionProviders;

        public CollectionRuleActionOptionsProvider(IEnumerable<ICollectionRuleActionProvider> actionProviders)
        {
            _actionProviders = actionProviders;
        }

        public bool TryGetOptionsType(string actionType, out Type optionsType)
        {
            ICollectionRuleActionProvider actionProvider = _actionProviders
                .FirstOrDefault(provider => string.Equals(actionType, provider.ActionType, StringComparison.Ordinal));
            if (null == actionProvider)
            {
                optionsType = null;
                return false;
            }

            optionsType = actionProvider.OptionsType;
            return true;
        }
    }
}
