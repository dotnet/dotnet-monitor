// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.ObjectFormatting;
using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.ObjectFormatting.Formatters.DebuggerDisplay;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.Linq;
using Xunit;
using static Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.ObjectFormatting.Formatters.DebuggerDisplay.DebuggerDisplayParser;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.UnitTests.ParameterCapturing.ObjectFormatting.Formatters.DebuggerDisplay
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class DebuggerDisplayParserTests
    {
        [Theory]
        // No expressions
        [InlineData("no_expressions", "no_expressions")]
        // Balanced expressions
        [InlineData("{valid} }invalid_expression", null)]
        [InlineData("{valid} {invalid_expression", null)]
        [InlineData("{{invalid_expression}}", null)]
        // Escape sequence
        [InlineData("\\{\\}", null)]
        // Method expressions
        [InlineData("Test: {methodName()}", "Test: {0}", "methodName()")]
        [InlineData("Test: {methodName(ArgName, SecondArg)}", "Test: {0}", "methodName(ArgName, SecondArg)")]
        // Property expressions
        [InlineData("Test: {propertyName}", "Test: {0}", "propertyName")]
        // Chained expressions
        [InlineData("Test: {methodName().propertyName.methodName()}", "Test: {0}", "methodName().propertyName.methodName()")]
        // Format specificers
        [InlineData("Test: {methodName(ArgName, SecondArg),raw,nq}", "Test: {0}", "methodName(ArgName, SecondArg)")]
        // Multiple expressions
        [InlineData("Test: {prop1} - {prop2} - {method()}", "Test: {0} - {1} - {2}", "prop1", "prop2", "method()")]
        // Complex expressions
        [InlineData("Test: {propertyName - 2}", "Test: {0}", "propertyName - 2")]
        public void ParseDebuggerDisplay(string debuggerDisplay, string? formatString, params string[] expressions)
        {
            // Act
            ParsedDebuggerDisplay? parsed = DebuggerDisplayParser.ParseDebuggerDisplay(debuggerDisplay);

            // Assert
            if (formatString == null)
            {
                Assert.Null(parsed);
                return;
            }

            Assert.NotNull(parsed);
            Assert.Equal(formatString, parsed.Value.FormatString);
            Assert.Equal(expressions, parsed.Value.Expressions.Select(p => p.ExpressionString.ToString()));
        }

        [Theory]
        [InlineData("{MyFunc(A,B),nq,raw}", "MyFunc(A,B)", FormatSpecifier.NoQuotes)]
        [InlineData("{(MyFunc(A,B)?.ToString()),nq,raw}", "(MyFunc(A,B)?.ToString())", FormatSpecifier.NoQuotes)]
        [InlineData("{MyFunc(\"5\")}", null, FormatSpecifier.None)]
        [InlineData("{MyFunc('5')}", null, FormatSpecifier.None)]
        [InlineData("{)(a)}", null, FormatSpecifier.None)]
        [InlineData("{((a)}", null, FormatSpecifier.None)]
        [InlineData("{\\}}", null, FormatSpecifier.None)]
        [InlineData("{a}", "a", FormatSpecifier.None)]
        internal void ParseExpression(string rawExpression, string? expressionString, FormatSpecifier formatSpecifier)
        {
            // Act
            Expression? expression = DebuggerDisplayParser.ParseExpression(rawExpression.AsMemory(), out _);

            // Assert
            if (expressionString == null)
            {
                Assert.Null(expression);
                return;
            }

            Assert.NotNull(expression);
            Assert.Equal(expressionString, expression.Value.ExpressionString.ToString());
            Assert.Equal(formatSpecifier, expression.Value.FormatSpecifier);
        }

        [Theory]
        [InlineData("nq,raw", FormatSpecifier.NoQuotes)]
        [InlineData(",nse,,,nq", FormatSpecifier.NoQuotes | FormatSpecifier.NoSideEffects)]
        [InlineData(",,,,", FormatSpecifier.None)]
        [InlineData("", FormatSpecifier.None)]
        [InlineData("nq,nse,nse,nq", FormatSpecifier.NoQuotes | FormatSpecifier.NoSideEffects)]
        [InlineData("nqa", FormatSpecifier.None)]
        [InlineData("NQ", FormatSpecifier.None)]
        internal void ParseFormatSpecifiers(string specifiersString, FormatSpecifier expectedSpecifier)
        {
            // Act
            FormatSpecifier actualSpecifier = DebuggerDisplayParser.ParseFormatSpecifiers(specifiersString);

            // Assert
            Assert.Equal(expectedSpecifier, actualSpecifier);
        }
    }
}
