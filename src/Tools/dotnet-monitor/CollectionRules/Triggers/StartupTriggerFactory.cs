// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration;
using Microsoft.Extensions.Configuration;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers
{
    /// <summary>
    /// Factory for creating a new Startup trigger.
    /// </summary>
    internal sealed class StartupTriggerFactory :
        ICollectionRuleTriggerFactory
    {
        /// <inheritdoc/>
        public ICollectionRuleTrigger Create(IEndpointInfo endpointInfo, Action callback)
        {
            return new StartupTrigger(callback);
        }
    }

    internal sealed class StartupTriggerDescriptor : ICollectionRuleTriggerDescriptor
    {
        public Type FactoryType => typeof(StartupTriggerFactory);
        public Type? OptionsType => null;
        public string TriggerName => KnownCollectionRuleTriggers.Startup;

        public bool TryBindOptions(IConfigurationSection settingsSection, out object? settings)
        {
            settings = null;
            return false;
        }
    }
}
