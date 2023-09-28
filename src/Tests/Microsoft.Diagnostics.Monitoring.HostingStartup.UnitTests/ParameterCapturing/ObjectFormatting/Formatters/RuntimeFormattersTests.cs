// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.ObjectFormatting;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using SampleMethods;
using System;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.UnitTests.ParameterCapturing.ObjectFormatting.Formatters
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class RuntimeFormattersTests
    {
        [Theory]
        [InlineData("test", "'test'", FormatSpecifier.None)]
        [InlineData("test", "test", FormatSpecifier.NoQuotes)]
        [InlineData(5, "5", FormatSpecifier.None)]
        [InlineData(true, "True", FormatSpecifier.None)]
        [InlineData(MyEnum.ValueA, nameof(MyEnum.ValueA), FormatSpecifier.None)]
        internal void IConvertibleFormatter(object obj, string expected, FormatSpecifier formatSpecifier)
        {
            // Act
            string actual = RuntimeFormatters.IConvertibleFormatter(obj, formatSpecifier);

            // Assert
            Assert.Equal(expected, actual);
        }


        [Theory]
        [InlineData("test", "'test'", FormatSpecifier.None)]
        [InlineData("test", "test", FormatSpecifier.NoQuotes)]
        internal void GeneralFormatter(object obj, string expected, FormatSpecifier formatSpecifier)
        {
            // Act
            string actual = RuntimeFormatters.GeneralFormatter(obj, formatSpecifier);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("'test 1000'", FormatSpecifier.None)]
        [InlineData("test 1000", FormatSpecifier.NoQuotes)]
        internal void IFormattableFormatter(string expected, FormatSpecifier formatSpecifier)
        {
            // Arrange
            int testFormatValue = 1000;
            FormattableString testFormattableObj = $"test {testFormatValue}";

            // Act
            string actual = RuntimeFormatters.IFormattableFormatter(testFormattableObj, formatSpecifier);

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}
