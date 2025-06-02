// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    internal interface ICollectionRuleActionDescriptor
    {
        string ActionName { get; }

        Type FactoryType { get; }

        Type OptionsType { get; }

        void BindOptions(IConfigurationSection settingsSection, out object settings);
    }

    internal interface ICollectionRuleActionDescriptor<TOptions, TFactory>
        : IValidateOptions<TOptions>, ICollectionRuleActionDescriptor
        where TOptions : class
        where TFactory : ICollectionRuleActionFactory<TOptions>
    {
        Type ICollectionRuleActionDescriptor.OptionsType => typeof(TOptions);

        Type ICollectionRuleActionDescriptor.FactoryType => typeof(TFactory);

        void BindOptions(IConfigurationSection settingsSection, out TOptions settings);

        void ICollectionRuleActionDescriptor.BindOptions(IConfigurationSection settingsSection, out object settings)
        {
            BindOptions(settingsSection, out TOptions typedSettings);
            settings = typedSettings;
        }
    }
}
