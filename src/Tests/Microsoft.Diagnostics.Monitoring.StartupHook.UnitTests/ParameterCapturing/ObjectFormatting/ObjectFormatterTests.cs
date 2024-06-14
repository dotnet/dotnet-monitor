// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.ObjectFormatting;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using Xunit;
using static Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing.ParameterCapturingEvents;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.UnitTests.ParameterCapturing
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class ObjectFormatterTests
    {
        [Fact]
        public void FormatObject_Handles_FaultingFormatter()
        {
            // Arrange
            ObjectFormatterFunc formatter = (object _, FormatSpecifier _) =>
            {
                throw new Exception("exception");
            };

            // Act
            ObjectFormatterResult actual = ObjectFormatter.FormatObject(formatter, 5);

            // Assert
            Assert.Equal(ObjectFormatter.Tokens.Exception, actual.FormattedValue);
            Assert.Equal(ParameterEvaluationResult.FailedEval, actual.EvalResult);
        }

        [Fact]
        public void FormatObject_Handles_NoSideEffects()
        {
            // Arrange
            ObjectFormatterFunc formatter = (object _, FormatSpecifier _) => { return new(string.Empty); };

            // Act
            ObjectFormatterResult actual = ObjectFormatter.FormatObject(formatter, 5, FormatSpecifier.NoSideEffects);

            // Assert
            Assert.Equal(ObjectFormatter.Tokens.CannotFormatWithoutSideEffects, actual.FormattedValue);
            Assert.Equal(ParameterEvaluationResult.EvalHasSideEffects, actual.EvalResult);
        }

        [Fact]
        public void FormatObject_Passes_FormatSpecifier()
        {
            // Arrange
            FormatSpecifier? actualSpecifier = null;
            ObjectFormatterFunc formatter = (object obj, FormatSpecifier specifier) =>
            {
                actualSpecifier = specifier;
                return new(string.Empty);
            };

            // Act
            ObjectFormatterResult actual = ObjectFormatter.FormatObject(formatter, 10, FormatSpecifier.NoQuotes);

            // Assert
            Assert.Equal(FormatSpecifier.NoQuotes, actualSpecifier);
        }
    }
}
