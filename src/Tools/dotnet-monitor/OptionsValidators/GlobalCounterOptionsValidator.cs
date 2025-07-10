// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    [OptionsValidator]
    partial class GlobalCounterOptionsValidator : IValidateOptions<GlobalCounterOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public GlobalCounterOptionsValidator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
    }
}
