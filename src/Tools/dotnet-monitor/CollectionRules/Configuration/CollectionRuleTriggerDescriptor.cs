// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    internal class CollectionRuleTriggerDescriptor<TFactory> :
        ICollectionRuleTriggerDescriptor
    {
        public CollectionRuleTriggerDescriptor(string triggerName) :
            this (triggerName, null)
        {
        }

        protected CollectionRuleTriggerDescriptor(string triggerName, Type optionsType)
        {
            TriggerName = triggerName;
            OptionsType = optionsType;
        }

        public Type FactoryType => typeof(TFactory);

        public Type OptionsType { get; }

        public string TriggerName { get; }
    }

    internal sealed class CollectionRuleTriggerProvider<TFactory, TOptions> :
        CollectionRuleTriggerDescriptor<TFactory>
    {
        public CollectionRuleTriggerProvider(string triggerName)
            : base(triggerName, typeof(TOptions))
        {
        }
    }
}
