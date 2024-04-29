// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using static Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing.ParameterCapturingEvents;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.ObjectFormatting
{
    internal delegate ObjectFormatterResult ObjectFormatterFunc(object obj, FormatSpecifier formatSpecifier = FormatSpecifier.None);

    // A subset of https://learn.microsoft.com/visualstudio/debugger/format-specifiers-in-csharp#format-specifiers
    [Flags]
    internal enum FormatSpecifier
    {
        None = 0,
        NoQuotes = 1,
        NoSideEffects = 2
    }

    internal readonly struct ObjectFormatterResult
    {
        public string FormattedValue { get; init; }

        public ParameterEvaluationFlags Flags { get; init; }

        public static readonly ObjectFormatterResult Empty = new() { FormattedValue = string.Empty, Flags = ParameterEvaluationFlags.None };
        public static readonly ObjectFormatterResult Null = new() { FormattedValue = ObjectFormatter.Tokens.Null, Flags = ParameterEvaluationFlags.IsNull };
        public static readonly ObjectFormatterResult Unsupported = new() { FormattedValue = ObjectFormatter.Tokens.Unsupported, Flags = ParameterEvaluationFlags.UnsupportedEval };
        public static readonly ObjectFormatterResult EvalException = new() { FormattedValue = ObjectFormatter.Tokens.Exception, Flags = ParameterEvaluationFlags.FailedEval };
        public static readonly ObjectFormatterResult EvalWithSideEffects = new() { FormattedValue = ObjectFormatter.Tokens.CannotFormatWithoutSideEffects, Flags = ParameterEvaluationFlags.EvalHasSideEffects };
    };

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

            public const string Unsupported = Internal.Prefix + "unsupported" + Internal.Postfix;
            public const string Exception = Internal.Prefix + "exception_thrown" + Internal.Postfix;
            public const string CannotFormatWithoutSideEffects = Internal.Prefix + "cannot_format_without_side_effects" + Internal.Postfix;
        }

        public static string WrapValue(string value) => string.Concat(
             Tokens.WrappedStart,
             value,
             Tokens.WrappedEnd);


        public static ObjectFormatterResult FormatObject(ObjectFormatterFunc formatterFunc, object obj, FormatSpecifier formatSpecifier = FormatSpecifier.None)
        {
            if ((formatSpecifier & FormatSpecifier.NoSideEffects) != 0)
            {
                return ObjectFormatterResult.EvalWithSideEffects;
            }

            try
            {
                return formatterFunc(obj, formatSpecifier);
            }
            catch
            {
                return ObjectFormatterResult.EvalException;
            }
        }
    }
}
