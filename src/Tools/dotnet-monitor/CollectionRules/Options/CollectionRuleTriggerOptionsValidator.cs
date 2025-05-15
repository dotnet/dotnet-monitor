// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    [OptionsValidator]
    partial class CollectionRuleTriggerOptionsValidator : IValidateOptions<CollectionRuleTriggerOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public CollectionRuleTriggerOptionsValidator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
    }
}
