// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    interface ICollectionRuleTriggerDescriptor
    {
        Type FactoryType { get; }

        Type? OptionsType { get; }

        string TriggerName { get; }

        bool TryBindOptions(IConfigurationSection settingsSection, out object? settings);
    }

    internal interface ICollectionRuleTriggerDescriptor<TOptions, TFactory>
        : IValidateOptions<TOptions>, ICollectionRuleTriggerDescriptor
        where TOptions : class
        where TFactory : ICollectionRuleTriggerFactory<TOptions>
    {
        Type ICollectionRuleTriggerDescriptor.OptionsType => typeof(TOptions);

        Type ICollectionRuleTriggerDescriptor.FactoryType => typeof(TFactory);

        bool TryBindOptions(IConfigurationSection settingsSection, out TOptions settings);

        bool ICollectionRuleTriggerDescriptor.TryBindOptions(IConfigurationSection settingsSection, out object? settings)
        {
            if (TryBindOptions(settingsSection, out TOptions typedSettings))
            {
                settings = typedSettings;
                return true;
            }
            settings = null;
            return false;
        }
    }
}
