// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.ObjectFormatting
{
    internal static class RuntimeFormatters
    {
        public static string IConvertibleFormatter(object obj, FormatSpecifier formatSpecifier)
        {
            if (obj is not string)
            {
                formatSpecifier |= FormatSpecifier.NoQuotes;
            }

            string formatted = ((IConvertible)obj).ToString(CultureInfo.InvariantCulture);
            return (formatSpecifier & FormatSpecifier.NoQuotes) == 0
                ? ObjectFormatter.WrapValue(formatted)
                : formatted;
        }

        public static string IFormattableFormatter(object obj, FormatSpecifier formatSpecifier)
        {
            string formatted = ((IFormattable)obj).ToString(format: null, CultureInfo.InvariantCulture);
            return (formatSpecifier & FormatSpecifier.NoQuotes) == 0
                ? ObjectFormatter.WrapValue(formatted)
                : formatted;
        }

        public static string GeneralFormatter(object obj, FormatSpecifier formatSpecifier)
        {
            string formatted = obj.ToString() ?? string.Empty;
            return (formatSpecifier & FormatSpecifier.NoQuotes) == 0
                ? ObjectFormatter.WrapValue(formatted)
                : formatted;
        }
    }
}
