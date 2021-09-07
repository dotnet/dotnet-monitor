// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using Microsoft.Diagnostics.Monitoring.EventPipe;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class PrometheusDataModel
    {
        private const char SeperatorChar = '_';

        private static readonly Dictionary<string, string> KnownUnits = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {string.Empty, string.Empty},
            {"count", string.Empty},
            {"B", "bytes" },
            {"MB", "bytes" },
            {"%", "ratio" },
        };

        public static string Normalize(string metricProvider, string metric, string unit, double value, out string metricValue)
        {
            string baseUnit = null;
            if ((unit != null) && (!KnownUnits.TryGetValue(unit, out baseUnit)))
            {
                baseUnit = unit;
            }
            if (string.Equals(unit, "MB", StringComparison.OrdinalIgnoreCase))
            {
                value *= 1_000_000; //Note that the metric uses MB not MiB
            }
            metricValue = value.ToString(CultureInfo.InvariantCulture);

            bool hasUnit = !string.IsNullOrEmpty(baseUnit);

            //The +1's account for separators
            //CONSIDER Can we optimize with Span/stackalloc here instead of using StringBuilder?
            StringBuilder builder = new StringBuilder(metricProvider.Length + metric.Length + (hasUnit ? baseUnit.Length + 1 : 0) + 1);

            NormalizeString(builder, metricProvider, isProvider: true);
            builder.Append(SeperatorChar);
            NormalizeString(builder, metric, isProvider: false);
            if (hasUnit)
            {
                builder.Append(SeperatorChar);
                NormalizeString(builder, baseUnit, isProvider: false);
            }

            return builder.ToString();
        }

        private static void NormalizeString(StringBuilder builder, string entity, bool isProvider)
        {
            //TODO We don't have any labels in the current metrics implementation, but may need to add support for it
            //for tags in the new dotnet metrics. Labels have some additional restrictions.

            bool allInvalid = true;
            for (int i = 0; i < entity.Length; i++)
            {
                if (IsValidChar(entity[i], i == 0))
                {
                    allInvalid = false;
                    builder.Append(isProvider ? char.ToLowerInvariant(entity[i]) : entity[i]);
                }
                else if (!isProvider)
                {
                    builder.Append(SeperatorChar);
                }
            }

            //CONSIDER Completely invalid providers such as '!@#$' will become '_'. Should we have a more obvious value for this?
            if (allInvalid && isProvider)
            {
                builder.Append(SeperatorChar);
            }
        }
        private static bool IsValidChar(char c, bool isFirst)
        {
            if (c > 'z')
            {
                return false;
            }

            if (c == SeperatorChar)
            {
                return true;
            }

            if (isFirst)
            {
                return char.IsLetter(c);
            }
            return char.IsLetterOrDigit(c);
        }
    }
}
