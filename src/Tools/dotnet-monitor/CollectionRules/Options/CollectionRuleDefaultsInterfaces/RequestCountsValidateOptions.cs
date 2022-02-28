// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.CollectionRuleDefaultsInterfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class RequestCountsValidateOptions<TOptions> :
        IValidateOptions<TOptions>
        where TOptions : class, RequestCounts
    {
        private readonly IServiceProvider _serviceProvider;

        public RequestCountsValidateOptions(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ValidateOptionsResult Validate(string name, TOptions options)
        {
            var collectionRuleDefaultOptions = _serviceProvider.GetService<IOptionsMonitor<CollectionRuleDefaultOptions>>();

            IList<string> failures = new List<string>();

            // Might need to set the options type to be (int?) so that we can null check instead of relying on a default 0 value
            if (null == options.RequestCount)
            {
                options.RequestCount = collectionRuleDefaultOptions.CurrentValue.RequestCount;

                if (null == options.RequestCount)
                {
                    // Need to push this to a string resource
                    failures.Add("No default request count and no request count given by user");
                    // FAIL if no default and nothing set by user
                    return ValidateOptionsResult.Fail(failures);
                }
            }

            return ValidateOptionsResult.Success;
        }
    }
}
