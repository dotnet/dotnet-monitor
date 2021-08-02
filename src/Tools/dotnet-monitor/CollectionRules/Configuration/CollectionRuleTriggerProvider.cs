// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    internal class CollectionRuleTriggerProvider :
        ICollectionRuleTriggerProvider
    {
        public CollectionRuleTriggerProvider(string triggerType) :
            this (triggerType, null)
        {
        }

        public CollectionRuleTriggerProvider(string triggerType, Type optionsType)
        {
            TriggerType = triggerType;
            OptionsType = optionsType;
        }

        public string TriggerType { get; }

        public Type OptionsType { get; }
    }

    internal sealed class CollectionRuleTriggerProvider<TOptions> :
        CollectionRuleTriggerProvider
    {
        public CollectionRuleTriggerProvider(string triggerType)
            : base(triggerType, typeof(TOptions))
        {
        }
    }
}
