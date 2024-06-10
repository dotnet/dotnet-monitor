// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.ObjectFormatting
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
            return new(formatSpecifier.HasFlag(FormatSpecifier.NoQuotes) ? formatted : ObjectFormatter.WrapValue(formatted));
        }

        public static ObjectFormatterResult IFormattableFormatter(object obj, FormatSpecifier formatSpecifier)
        {
            string formatted = ((IFormattable)obj).ToString(format: null, CultureInfo.InvariantCulture);
            return new(formatSpecifier.HasFlag(FormatSpecifier.NoQuotes) ? formatted : ObjectFormatter.WrapValue(formatted));
        }

        public static ObjectFormatterResult GeneralFormatter(object obj, FormatSpecifier formatSpecifier)
        {
            string formatted = obj.ToString() ?? string.Empty;
            return new(formatSpecifier.HasFlag(FormatSpecifier.NoQuotes) ? formatted : ObjectFormatter.WrapValue(formatted));
        }
    }
}
