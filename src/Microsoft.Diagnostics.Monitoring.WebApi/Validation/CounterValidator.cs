﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System.Globalization;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Validation
{
    internal static class CounterValidator
    {
        public static bool ValidateProvider(GlobalCounterOptions counterOptions,
            EventPipeProvider provider,
            out string errorMessage)
        {
            errorMessage = null;

            if (provider.Arguments?.TryGetValue("EventCounterIntervalSec", out string intervalValue) == true)
            {
                if (float.TryParse(intervalValue, out float intervalSeconds) &&
                    intervalSeconds != counterOptions.GetIntervalSeconds())
                {
                    errorMessage = string.Format(CultureInfo.CurrentCulture,
                        Strings.ErrorMessage_InvalidMetricInterval,
                        provider.Name,
                        counterOptions.GetIntervalSeconds());
                    return false;
                }
            }

            return true;
        }
    }
}
