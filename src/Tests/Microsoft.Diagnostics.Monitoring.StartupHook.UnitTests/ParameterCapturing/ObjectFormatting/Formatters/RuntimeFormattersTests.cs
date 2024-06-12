// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.ObjectFormatting;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using SampleMethods;
using System;
using Xunit;
using static Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing.ParameterCapturingEvents;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.UnitTests.ParameterCapturing.ObjectFormatting.Formatters
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class RuntimeFormattersTests
    {
        [Theory]
        [InlineData("test", "'test'", ParameterEvaluationResult.Success, FormatSpecifier.None)]
        [InlineData("test", "test", ParameterEvaluationResult.Success, FormatSpecifier.NoQuotes)]
        [InlineData(5, "5", ParameterEvaluationResult.Success, FormatSpecifier.None)]
        [InlineData(true, "True", ParameterEvaluationResult.Success, FormatSpecifier.None)]
        [InlineData(MyEnum.ValueA, nameof(MyEnum.ValueA), ParameterEvaluationResult.Success, FormatSpecifier.None)]
        internal void IConvertibleFormatter(object obj, string expectedFormattedValue, ParameterEvaluationResult expectedEvaluationResult, FormatSpecifier formatSpecifier)
        {
            // Act
            ObjectFormatterResult actual = RuntimeFormatters.IConvertibleFormatter(obj, formatSpecifier);

            // Assert
            Assert.Equal(expectedFormattedValue, actual.FormattedValue);
            Assert.Equal(expectedEvaluationResult, actual.EvalResult);
        }


        [Theory]
        [InlineData("test", "'test'", ParameterEvaluationResult.Success, FormatSpecifier.None)]
        [InlineData("test", "test", ParameterEvaluationResult.Success, FormatSpecifier.NoQuotes)]
        internal void GeneralFormatter(object obj, string expectedFormattedValue, ParameterEvaluationResult expectedEvaluationResult, FormatSpecifier formatSpecifier)
        {
            // Act
            ObjectFormatterResult actual = RuntimeFormatters.GeneralFormatter(obj, formatSpecifier);

            // Assert
            Assert.Equal(expectedFormattedValue, actual.FormattedValue);
            Assert.Equal(expectedEvaluationResult, actual.EvalResult);
        }

        [Theory]
        [InlineData("'test 1000'", ParameterEvaluationResult.Success, FormatSpecifier.None)]
        [InlineData("test 1000", ParameterEvaluationResult.Success, FormatSpecifier.NoQuotes)]
        internal void IFormattableFormatter(string expectedFormattedValue, ParameterEvaluationResult expectedEvaluationResult, FormatSpecifier formatSpecifier)
        {
            // Arrange
            int testFormatValue = 1000;
            FormattableString testFormattableObj = $"test {testFormatValue}";

            // Act
            ObjectFormatterResult actual = RuntimeFormatters.IFormattableFormatter(testFormattableObj, formatSpecifier);

            // Assert
            Assert.Equal(expectedFormattedValue, actual.FormattedValue);
            Assert.Equal(expectedEvaluationResult, actual.EvalResult);
        }
    }
}
