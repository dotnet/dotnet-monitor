// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.CollectionRuleDefaultsInterfaces;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class SlidingWindowDurationValidateOptions<TOptions> :
        IValidateOptions<TOptions>
        where TOptions : class, SlidingWindowDurations
    {
        private readonly IServiceProvider _serviceProvider;

        public SlidingWindowDurationValidateOptions(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Validate might be a misnomer here - just choosing which default to use depending on whether the collection rule default is set
        public ValidateOptionsResult Validate(string name, TOptions options)
        {
            var collectionRuleDefaultOptions = _serviceProvider.GetService<IOptionsMonitor<CollectionRuleDefaultOptions>>();

            if (null == options.SlidingWindowDuration)
            {
                options.SlidingWindowDuration = collectionRuleDefaultOptions.CurrentValue.SlidingWindowDuration;

                if (null == options.SlidingWindowDuration)
                {
                    options.SlidingWindowDuration = TimeSpan.Parse(TriggerOptionsConstants.SlidingWindowDuration_Default);
                }
            }

            return ValidateOptionsResult.Success;
        }
    }
}
