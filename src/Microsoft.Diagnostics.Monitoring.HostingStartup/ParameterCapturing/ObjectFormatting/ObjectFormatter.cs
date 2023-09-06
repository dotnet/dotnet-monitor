// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.ObjectFormatting
{
    internal delegate string ObjectFormatterFunc(object obj, FormatSpecifier formatSpecifier = FormatSpecifier.None);

    [Flags]
    internal enum FormatSpecifier
    {
        None = 0,
        NoQuotes = 1
    }

    internal static class ObjectFormatter
    {
        public static class Tokens
        {
            public const string Null = "null";
            public const char WrappedStart = '\'';
            public const char WrappedEnd = '\'';

            private static class Internal
            {
                public const string Prefix = "<";
                public const string Postfix = ">";
            }

            public const string Exception = Internal.Prefix + "exception_thrown" + Internal.Postfix;
        }

        public static string WrapValue(string value) => string.Concat(
             Tokens.WrappedStart,
             value,
             Tokens.WrappedEnd);


        public static string FormatObject(ObjectFormatterFunc formatterFunc, object obj, FormatSpecifier formatSpecifier = FormatSpecifier.None)
        {
            try
            {
                return formatterFunc(obj, formatSpecifier);
            }
            catch
            {
                return Tokens.Exception;
            }
        }
    }
}
