// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
#else
namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
#endif
{
    partial record class CollectLogsOptions :
        IValidatableObject
    {
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            List<ValidationResult> results = new();

            if (null != FilterSpecs)
            {
                RequiredAttribute requiredAttribute = new();
                EnumDataTypeAttribute enumValidationAttribute = new(typeof(LogLevel));

                ValidationContext filterSpecsContext = new(FilterSpecs, validationContext, validationContext.Items);
                filterSpecsContext.MemberName = nameof(FilterSpecs);

                // Validate that the category is not null and that the level is a valid level value.
                foreach ((string category, LogLevel? level) in FilterSpecs)
                {
                    ValidationResult? result = requiredAttribute.GetValidationResult(category, filterSpecsContext);
                    if (!result.IsSuccess())
                    {
                        results.Add(result);
                    }

                    result = enumValidationAttribute.GetValidationResult(level, filterSpecsContext);
                    if (!result.IsSuccess())
                    {
                        results.Add(result);
                    }
                }
            }

            return results;
        }
    }
}
