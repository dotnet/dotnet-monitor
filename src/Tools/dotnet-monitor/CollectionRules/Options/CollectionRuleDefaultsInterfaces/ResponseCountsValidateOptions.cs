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
    // This file can likely be relocated as the validation file for AspNetResponseStatusOptions -> can then get rid of the singleton for it and the interface
    internal sealed class ResponseCountsValidateOptions<TOptions> :
        IValidateOptions<TOptions>
        where TOptions : class, ResponseCounts
    {
        private readonly IServiceProvider _serviceProvider;

        public ResponseCountsValidateOptions(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ValidateOptionsResult Validate(string name, TOptions options)
        {
            var collectionRuleDefaultOptions = _serviceProvider.GetService<IOptionsMonitor<CollectionRuleDefaultOptions>>();

            IList<string> failures = new List<string>();

            if (null == options.ResponseCount)
            {
                options.ResponseCount = collectionRuleDefaultOptions.CurrentValue.ResponseCount;

                if (null == options.ResponseCount)
                {
                    // Need to push this to a string resource
                    failures.Add("No default response count and no response count given by user");
                    // FAIL if no default and nothing set by user
                    return ValidateOptionsResult.Fail(failures);
                }
            }

            // Do we care if it's an empty array -> does that mean we accept all status codes?
            if (null == options.StatusCodes)
            {
                options.StatusCodes = collectionRuleDefaultOptions.CurrentValue.StatusCodes;

                if (null == options.StatusCodes)
                {
                    // Need to push this to a string resource
                    failures.Add("No default status codes and no status codes given by user");
                    // FAIL if no default and nothing set by user
                    return ValidateOptionsResult.Fail(failures);
                }
            }

            return ValidateOptionsResult.Success;
        }
    }
}
