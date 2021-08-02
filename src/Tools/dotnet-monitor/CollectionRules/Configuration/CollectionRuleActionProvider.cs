// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    internal sealed class CollectionRuleActionProvider<TOptions> :
        ICollectionRuleActionProvider
    {
        public CollectionRuleActionProvider(string actionType)
        {
            ActionType = actionType;
            OptionsType = typeof(TOptions);
        }

        public string ActionType { get; }

        public Type OptionsType { get; }
    }
}
