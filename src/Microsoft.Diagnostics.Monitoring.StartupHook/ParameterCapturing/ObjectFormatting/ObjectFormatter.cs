// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using static Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing.ParameterCapturingEvents;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.ObjectFormatting
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

    internal readonly struct ObjectFormatterResult(string value, ParameterEvaluationResult evalResult)
    {
        public ObjectFormatterResult(string value) : this(value, ParameterEvaluationResult.Success)
        {
        }

        public string FormattedValue { get; } = value;

        public ParameterEvaluationResult EvalResult { get; } = evalResult;

        public static readonly ObjectFormatterResult Null = new(ObjectFormatter.Tokens.Null, ParameterEvaluationResult.IsNull);
        public static readonly ObjectFormatterResult Unsupported = new(ObjectFormatter.Tokens.Unsupported, ParameterEvaluationResult.UnsupportedEval);
        public static readonly ObjectFormatterResult EvalException = new(ObjectFormatter.Tokens.Exception, ParameterEvaluationResult.FailedEval);
        public static readonly ObjectFormatterResult EvalWithSideEffects = new(ObjectFormatter.Tokens.CannotFormatWithoutSideEffects, ParameterEvaluationResult.EvalHasSideEffects);
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
