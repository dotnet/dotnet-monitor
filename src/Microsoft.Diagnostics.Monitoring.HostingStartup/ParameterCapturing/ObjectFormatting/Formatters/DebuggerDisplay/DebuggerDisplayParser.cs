// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.ObjectFormatting.Formatters.DebuggerDisplay
{
    internal static class DebuggerDisplayParser
    {
        internal record ParsedDebuggerDisplay(string FormatString, List<Expression> Expressions);
        internal record Expression(ReadOnlyMemory<char> ExpressionString, FormatSpecifier FormatSpecifier);

        internal static ParsedDebuggerDisplay? ParseDebuggerDisplay(string debuggerDisplay)
        {
            //
            // A debugger display value is a string with expressions inside that can be evaluated.
            // This method will transformthe debugger display into a standard format string
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
                    case '\\':
                        // Escape sequence is not currently supported.
                        return null;
                        
                    case '{':
                        // Encountered the start of an expression, try to parse it and replace it with a standard format item.
                        Expression? parsedExpression = ParseExpression(debuggerDisplay.AsMemory(i), out int charsRead);
                        if (parsedExpression == null || charsRead <= 0)
                        {
                            return null;
                        }

                        // Skip past the parsed expression, account for the fact that '{' gets read twice
                        // (once by this method, and once by ParseExpression).
                        i += (charsRead - 1);

                        fmtString.Append('{');
                        fmtString.Append(expressions.Count);
                        fmtString.Append('}');

                        expressions.Add(parsedExpression);

                        break;
                    case '}':
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
            if (spanExpression[0] != '{')
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
                    case '\\':
                        // Escape sequence is not currently supported.
                        return null;

                    case '(':
                        parenthesisDepth++;
                        break;

                    case ')':
                        if (parenthesisDepth-- < 0)
                        {
                            // Unbalanced parenthesis, malformed expression
                            return null;
                        }
                        break;

                    case '{':
                        // Malformed, the start of this expression has been processed already
                        return null;

                    case '}':
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

                    case ',':
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

                if (specifier.Equals("nq", StringComparison.Ordinal))
                {
                    formatSpecifier |= FormatSpecifier.NoQuotes;
                }
                else if (specifier.Equals("nse", StringComparison.Ordinal))
                {
                    formatSpecifier |= FormatSpecifier.NoSideEffects;
                }
            }

            int startIndex = 0;
            for (int i = 0; i < specifiers.Length; i++)
            {
                char c = specifiers[i];

                if (c == ',')
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
