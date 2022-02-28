// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    partial class CollectionRuleLimitsOptions : IValidatableObject
    {
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            List<ValidationResult> results = new(); // Not being used since not actually validating; just setting defaults

            var collectionRuleDefaultOptions = validationContext.GetService<IOptionsMonitor<CollectionRuleDefaultOptions>>();

            if (null == RuleDuration)
            {
                RuleDuration = collectionRuleDefaultOptions.CurrentValue.RuleDuration;
            }

            if (null == ActionCountSlidingWindowDuration)
            {
                ActionCountSlidingWindowDuration = collectionRuleDefaultOptions.CurrentValue.ActionCountSlidingWindowDuration;
            }

            // Checking null here probably works because it's (int?) but need to check that
            if (null == ActionCount)
            {
                ActionCount = collectionRuleDefaultOptions.CurrentValue.ActionCount;

                // Might also need to check against 00:00:00 here...?
                if (null == ActionCount)
                {
                    ActionCount = CollectionRuleLimitsOptionsDefaults.ActionCount;
                }
            }

            return results;
        }
    }
}
