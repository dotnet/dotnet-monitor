// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class PrometheusDataModel
    {
        private const char SeparatorChar = '_';
        private const char EqualsChar = '=';
        private const char QuotationChar = '"';
        private const char SlashChar = '\\';
        private const char NewlineChar = '\n';

        private static readonly Dictionary<string, string> KnownUnits = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {string.Empty, string.Empty},
            {"count", string.Empty},
            {"B", "bytes" },
            {"MB", "bytes" },
            {"%", "ratio" },
        };

        public static string GetPrometheusNormalizedName(string metricProvider, string metric, string unit)
        {
            string? baseUnit = null;
            if ((unit != null) && (!KnownUnits.TryGetValue(unit, out baseUnit)))
            {
                baseUnit = unit;
            }

            //The +1's account for separators
            //CONSIDER Can we optimize with Span/stackalloc here instead of using StringBuilder?
            StringBuilder builder = new StringBuilder(metricProvider.Length + metric.Length + (!string.IsNullOrEmpty(baseUnit) ? baseUnit.Length + 1 : 0) + 1);

            NormalizeString(builder, metricProvider, isProvider: true);
            builder.Append(SeparatorChar);
            NormalizeString(builder, metric, isProvider: false);
            if (!string.IsNullOrEmpty(baseUnit))
            {
                builder.Append(SeparatorChar);
                NormalizeString(builder, baseUnit, isProvider: false);
            }

            return builder.ToString();
        }

        public static string GetPrometheusNormalizedLabel(string key, string value)
        {
            StringBuilder builder = new StringBuilder(key.Length + 2 * value.Length + 3); // Includes =,", and ", as well as extra padding for potential escape characters in the value

            NormalizeString(builder, key, isProvider: false);
            builder.Append(EqualsChar);
            builder.Append(QuotationChar);
            NormalizeLabelValue(builder, value);
            builder.Append(QuotationChar);

            return builder.ToString();
        }

        public static string GetPrometheusNormalizedValue(string unit, double value)
        {
            if (string.Equals(unit, "MB", StringComparison.OrdinalIgnoreCase))
            {
                value *= 1_000_000; //Note that the metric uses MB not MiB
            }
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static void NormalizeLabelValue(StringBuilder builder, string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == SlashChar)
                {
                    builder.Append(SlashChar);
                    builder.Append(SlashChar);
                }
                else if (value[i] == NewlineChar)
                {
                    builder.Append(SlashChar);
                    builder.Append('n');
                }
                else if (value[i] == QuotationChar)
                {
                    builder.Append(SlashChar);
                    builder.Append(QuotationChar);
                }
                else
                {
                    builder.Append(value[i]);
                }
            }
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
                    builder.Append(SeparatorChar);
                }
            }

            //CONSIDER Completely invalid providers such as '!@#$' will become '_'. Should we have a more obvious value for this?
            if (allInvalid && isProvider)
            {
                builder.Append(SeparatorChar);
            }
        }
        private static bool IsValidChar(char c, bool isFirst)
        {
            if (c > 'z')
            {
                return false;
            }

            if (c == SeparatorChar)
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
