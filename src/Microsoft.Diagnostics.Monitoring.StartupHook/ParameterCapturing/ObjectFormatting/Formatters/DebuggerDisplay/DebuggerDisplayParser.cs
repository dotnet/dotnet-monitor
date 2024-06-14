// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.ObjectFormatting.Formatters.DebuggerDisplay
{
    internal static class DebuggerDisplayParser
    {
        internal record struct ParsedDebuggerDisplay(string FormatString, List<Expression> Expressions);
        internal record struct Expression(ReadOnlyMemory<char> ExpressionString, FormatSpecifier FormatSpecifier);

        private static class Tokens
        {
            public const char EscapeSequence = '\\';

            public static class Expression
            {
                public const char Start = '{';
                public const char End = '}';

                public const char ParenthesisStart = '(';
                public const char ParenthesisEnd = ')';

                public const char CharWrapper = '\'';
                public const char StringWrapper = '"';
            }

            public static class FormatString
            {
                public const char ItemStart = '{';
                public const char ItemEnd = '}';
            }

            public static class FormatSpecifier
            {
                public const char Delimiter = ',';

                public const string NoQuotes = "nq";
                public const string NoSideEffects = "nse";
            }
        }

        internal static ParsedDebuggerDisplay? ParseDebuggerDisplay(string debuggerDisplay)
        {
            //
            // A debugger display value is a string with expressions inside that can be evaluated.
            // This method will transform the debugger display into a standard format string
            // and extract the expressions.
            //
            // Simplified example:
            // Input: "Count = {Count}, Info = {DebuggerInfo()}"
            // Output: FormatString="Count = {0}, Info = {1}", Expressions=["Count", "DebuggerInfo()"]
            //
            StringBuilder fmtString = new();
            List<Expression> expressions = new();

            for (int i = 0; i < debuggerDisplay.Length; i++)
            {
                char c = debuggerDisplay[i];
                switch (c)
                {
                    case Tokens.EscapeSequence:
                        // Escape sequence is not currently supported.
                        return null;
                    case Tokens.Expression.Start:
                        // Encountered the start of an expression, try to parse it and replace it with a standard format item.
                        Expression? parsedExpression = ParseExpression(debuggerDisplay.AsMemory(i), out int charsRead);
                        if (parsedExpression == null || charsRead <= 0)
                        {
                            return null;
                        }

                        // Skip past the parsed expression, account for the fact that '{' gets read twice
                        // (once by this method, and once by ParseExpression).
                        i += (charsRead - 1);

                        fmtString.Append(Tokens.FormatString.ItemStart);
                        fmtString.Append(expressions.Count);
                        fmtString.Append(Tokens.FormatString.ItemEnd);

                        expressions.Add(parsedExpression.Value);

                        break;
                    case Tokens.Expression.End:
                        // Malformed if observed here since above ParseExpression will read the expression's terminating '}'.
                        return null;

                    default:
                        fmtString.Append(c);
                        break;
                }
            }

            return new ParsedDebuggerDisplay(fmtString.ToString(), expressions);
        }

        internal static Expression? ParseExpression(ReadOnlyMemory<char> expression, out int charsRead)
        {
            //
            // An expression inside a debugger display:
            // - Is encapsulated inside curly braces {..},
            // - Is followed by one or more format specifiers (delimited by a comma (,))
            //
            // This method is given the start of an expression, and is responsible for reading up until
            // the end of the expression.
            //
            // Simplified example:
            // Input: "{DebuggerInfo(),nq,nse}"
            // Output: ExpressionString="DebuggerInfo()", FormatSpecifier=FormatSpecifier.NoQuotes | FormatSpecifier.NoSideEffects
            //
            charsRead = 0;
            if (expression.IsEmpty)
            {
                return null;
            }

            // Ensure the first char is the start of an expression.
            ReadOnlySpan<char> spanExpression = expression.Span;
            if (spanExpression[0] != Tokens.Expression.Start)
            {
                return null;
            }
            const int expressionStartIndex = 1;
            charsRead++;

            // Keep track of where the format specifiers start so we know what span of chars to parse for it.
            int formatSpecifiersStart = -1;

            //
            // Keep track of the depth of parenthesis. This is used to:
            // - Identify malformed expressions (the parenthesis will be unbalanced).
            // - Identify when a comma denotes the start of the format specifiers
            //   e.g. "MyFunc(A, B),nq" -- nq is the format specifier since it's not encapsulated by any parenthesis.
            //
            int parenthesisDepth = 0;

            for (int i = expressionStartIndex; i < spanExpression.Length; i++)
            {
                charsRead++;
                char c = spanExpression[i];

                switch (c)
                {
                    case Tokens.Expression.StringWrapper: // Usage of strings or chars in an expression is not supported (complex expressions or methods with constant arguments)
                    case Tokens.Expression.CharWrapper:
                    case Tokens.EscapeSequence: // Escape sequence is not currently supported.
                        return null;

                    case Tokens.Expression.ParenthesisStart:
                        parenthesisDepth++;
                        break;

                    case Tokens.Expression.ParenthesisEnd:
                        if (parenthesisDepth-- < 0)
                        {
                            // Unbalanced parenthesis, malformed expression
                            return null;
                        }
                        break;

                    case Tokens.Expression.Start:
                        // Malformed, the start of this expression has been processed already
                        return null;

                    case Tokens.Expression.End:
                        // End of expression or malformed
                        if (parenthesisDepth != 0)
                        {
                            return null;
                        }

                        if (formatSpecifiersStart != -1)
                        {
                            return new Expression(
                                expression[expressionStartIndex..formatSpecifiersStart],
                                ParseFormatSpecifiers(spanExpression[formatSpecifiersStart..i]));
                        }

                        return new Expression(
                            expression[expressionStartIndex..i],
                            FormatSpecifier.None);

                    case Tokens.FormatSpecifier.Delimiter:
                        // Capture the start of the format specifiers.
                        // The entire set of format specifiers will be parsed later.
                        if (parenthesisDepth == 0 && formatSpecifiersStart == -1)
                        {
                            formatSpecifiersStart = i;
                        }
                        break;
                    default:
                        break;

                }
            }

            return null;
        }

        internal static FormatSpecifier ParseFormatSpecifiers(ReadOnlySpan<char> specifiers)
        {
            //
            // Format specifiers in an expression consist of one or more well-known tokens delimited by a comma.
            // - If multiple specifiers are set, they are all used.
            // - Format specifier tokens are case sensitive.
            //
            FormatSpecifier formatSpecifier = FormatSpecifier.None;

            void parseSpecifier(ReadOnlySpan<char> specifier)
            {
                if (specifier.Length == 0)
                {
                    return;
                }

                if (specifier.Equals(Tokens.FormatSpecifier.NoQuotes, StringComparison.Ordinal))
                {
                    formatSpecifier |= FormatSpecifier.NoQuotes;
                }
                else if (specifier.Equals(Tokens.FormatSpecifier.NoSideEffects, StringComparison.Ordinal))
                {
                    formatSpecifier |= FormatSpecifier.NoSideEffects;
                }
            }

            int startIndex = 0;
            for (int i = 0; i < specifiers.Length; i++)
            {
                char c = specifiers[i];

                if (c == Tokens.FormatSpecifier.Delimiter)
                {
                    parseSpecifier(specifiers[startIndex..i]);
                    startIndex = i + 1;
                    continue;
                }
            }

            parseSpecifier(specifiers[startIndex..specifiers.Length]);
            return formatSpecifier;
        }
    }
}
