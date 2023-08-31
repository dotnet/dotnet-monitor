// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.ObjectFormatter
{
    internal delegate string ObjectFormatter(object obj);

    /// <summary>
    /// The results from FormatterFactoryResult.
    /// </summary>
    /// <param name="Formatter">The object formatter.</param>
    /// <param name="MatchingTypes">Known types that this formatter will work against (including the requested type).</param>
    internal record FormatterFactoryResult(ObjectFormatter Formatter, IEnumerable<Type> MatchingTypes);


    [Flags]
    internal enum FormatSpecifier
    {
        None = 0,
        NoQuotes = 1
    }

    internal static class ObjectFormatterFactory
    {
        private static string WrapValue(string value) => string.Concat(
            MethodTemplateStringGenerator.Tokens.Parameters.Values.WrappedStart,
            value,
            MethodTemplateStringGenerator.Tokens.Parameters.Values.WrappedEnd);

        private static string IConvertibleFormatter(object obj, FormatSpecifier formatSpecifier)
        {
            string formatted = ((IConvertible)obj).ToString(CultureInfo.InvariantCulture);
            return (formatSpecifier & FormatSpecifier.NoQuotes) == 0
                ? WrapValue(formatted)
                : formatted;
        }

        private static string IFormattableFormatter(object obj, FormatSpecifier formatSpecifier)
        {
            string formatted = ((IFormattable)obj).ToString(format: null, CultureInfo.InvariantCulture);
            return (formatSpecifier & FormatSpecifier.NoQuotes) == 0
                ? WrapValue(formatted)
                : formatted;
        }

        private static string GeneralFormatter(object obj, FormatSpecifier formatSpecifier)
        {
            string formatted = obj.ToString() ?? string.Empty;
            return (formatSpecifier & FormatSpecifier.NoQuotes) == 0
                ? WrapValue(formatted)
                : formatted;
        }

        public static FormatterFactoryResult GetFormatter(Type objType, FormatSpecifier formatSpecifier = FormatSpecifier.None)
        {
            if (objType.IsAssignableTo(typeof(IConvertible)))
            {
                if (objType != typeof(string))
                {
                    formatSpecifier |= FormatSpecifier.NoQuotes;
                }

                return new FormatterFactoryResult((obj) => IConvertibleFormatter(obj, formatSpecifier), new[] { objType });
            }
            else if (objType.IsAssignableTo(typeof(IFormatProvider)))
            {
                return new FormatterFactoryResult((obj) => IFormattableFormatter(obj, formatSpecifier), new[] { objType });
            }
            else
            {
                return new FormatterFactoryResult((obj) => GeneralFormatter(obj, formatSpecifier), new[] { objType });
            }
        }
    }
}
