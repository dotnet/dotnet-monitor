// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.ObjectFormatting;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using SampleMethods;
using System;
using Xunit;
using static Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing.ParameterCapturingEvents;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.UnitTests.ParameterCapturing.ObjectFormatting.Formatters
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class RuntimeFormattersTests
    {
        [Theory]
        [InlineData("test", "'test'", ParameterEvaluationFlags.None, FormatSpecifier.None)]
        [InlineData("test", "test", ParameterEvaluationFlags.None, FormatSpecifier.NoQuotes)]
        [InlineData(5, "5", ParameterEvaluationFlags.None, FormatSpecifier.None)]
        [InlineData(true, "True", ParameterEvaluationFlags.None, FormatSpecifier.None)]
        [InlineData(MyEnum.ValueA, nameof(MyEnum.ValueA), ParameterEvaluationFlags.None, FormatSpecifier.None)]
        internal void IConvertibleFormatter(object obj, string expectedFormattedValue, ParameterEvaluationFlags expectedEvaluationFlags, FormatSpecifier formatSpecifier)
        {
            // Act
            ObjectFormatterResult actual = RuntimeFormatters.IConvertibleFormatter(obj, formatSpecifier);

            // Assert
            Assert.Equal(expectedFormattedValue, actual.FormattedValue);
            Assert.Equal(expectedEvaluationFlags, actual.Flags);
        }


        [Theory]
        [InlineData("test", "'test'", ParameterEvaluationFlags.None, FormatSpecifier.None)]
        [InlineData("test", "test", ParameterEvaluationFlags.None, FormatSpecifier.NoQuotes)]
        internal void GeneralFormatter(object obj, string expectedFormattedValue, ParameterEvaluationFlags expectedEvaluationFlags, FormatSpecifier formatSpecifier)
        {
            // Act
            ObjectFormatterResult actual = RuntimeFormatters.GeneralFormatter(obj, formatSpecifier);

            // Assert
            Assert.Equal(expectedFormattedValue, actual.FormattedValue);
            Assert.Equal(expectedEvaluationFlags, actual.Flags);
        }

        [Theory]
        [InlineData("'test 1000'", ParameterEvaluationFlags.None, FormatSpecifier.None)]
        [InlineData("test 1000", ParameterEvaluationFlags.None, FormatSpecifier.NoQuotes)]
        internal void IFormattableFormatter(string expectedFormattedValue, ParameterEvaluationFlags expectedEvaluationFlags, FormatSpecifier formatSpecifier)
        {
            // Arrange
            int testFormatValue = 1000;
            FormattableString testFormattableObj = $"test {testFormatValue}";

            // Act
            ObjectFormatterResult actual = RuntimeFormatters.IFormattableFormatter(testFormattableObj, formatSpecifier);

            // Assert
            Assert.Equal(expectedFormattedValue, actual.FormattedValue);
            Assert.Equal(expectedEvaluationFlags, actual.Flags);
        }
    }
}
