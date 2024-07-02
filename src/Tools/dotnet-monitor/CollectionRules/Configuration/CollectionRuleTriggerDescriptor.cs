// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    internal sealed class CollectionRuleTriggerDescriptor<TFactory> :
        ICollectionRuleTriggerDescriptor
        where TFactory : ICollectionRuleTriggerFactory
    {
        public CollectionRuleTriggerDescriptor(string triggerName)
        {
            TriggerName = triggerName;
        }

        public Type FactoryType => typeof(TFactory);

        public Type? OptionsType => null;

        public string TriggerName { get; }
    }

    internal sealed class CollectionRuleTriggerProvider<TFactory, TOptions> :
        ICollectionRuleTriggerDescriptor
        where TFactory : ICollectionRuleTriggerFactory<TOptions>
    {
        public CollectionRuleTriggerProvider(string triggerName)
        {
            TriggerName = triggerName;
        }

        public Type FactoryType => typeof(TFactory);

        public Type OptionsType => typeof(TOptions);

        public string TriggerName { get; }
    }
}
