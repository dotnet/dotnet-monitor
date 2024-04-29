// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using static Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing.ParameterCapturingEvents;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.ObjectFormatting
{
    internal static class RuntimeFormatters
    {
        public static ObjectFormatterResult IConvertibleFormatter(object obj, FormatSpecifier formatSpecifier)
        {
            if (obj is not string)
            {
                formatSpecifier |= FormatSpecifier.NoQuotes;
            }

            string formatted = ((IConvertible)obj).ToString(CultureInfo.InvariantCulture);
            return new()
            {
                FormattedValue = (formatSpecifier & FormatSpecifier.NoQuotes) == 0
                    ? ObjectFormatter.WrapValue(formatted)
                    : formatted,
                Flags = ParameterEvaluationFlags.None
            };
        }

        public static ObjectFormatterResult IFormattableFormatter(object obj, FormatSpecifier formatSpecifier)
        {
            string formatted = ((IFormattable)obj).ToString(format: null, CultureInfo.InvariantCulture);
            return new()
            {
                FormattedValue = (formatSpecifier & FormatSpecifier.NoQuotes) == 0
                    ? ObjectFormatter.WrapValue(formatted)
                    : formatted,
                Flags = ParameterEvaluationFlags.None
            };
        }

        public static ObjectFormatterResult GeneralFormatter(object obj, FormatSpecifier formatSpecifier)
        {
            string formatted = obj.ToString() ?? string.Empty;
            return new()
            {
                FormattedValue = (formatSpecifier & FormatSpecifier.NoQuotes) == 0
                    ? ObjectFormatter.WrapValue(formatted)
                    : formatted,
                Flags = ParameterEvaluationFlags.None
            };
        }
    }
}
