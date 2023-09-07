// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.ObjectFormatting;
using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.ObjectFormatting.Formatters.DebuggerDisplay;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.Diagnostics;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.UnitTests.ParameterCapturing.ObjectFormatting.Formatters.DebuggerDisplay
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class DebuggerDisplayFormatterTests
    {
        [DebuggerDisplay("Count = {Count}")]
        private class DebuggerDisplayClass
        {
            public int Count { get; set; }
        }
        private sealed class DerivedWithBaseDebuggerDisplay : DebuggerDisplayClass { }
        private sealed class NoDebuggerDisplay { }

        [Theory]
        [InlineData(typeof(NoDebuggerDisplay), null)]
        [InlineData(typeof(DebuggerDisplayClass), "Count = {Count}")]
        [InlineData(typeof(DerivedWithBaseDebuggerDisplay), "Count = {Count}")]
        public void GetDebuggerDisplayAttribute(Type type, string expected)
        {
            // Act
            string actual = DebuggerDisplayFormatter.GetDebuggerDisplayAttribute(type)?.Value;

            // Assert
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(typeof(NoDebuggerDisplay), null)]
        [InlineData(typeof(DebuggerDisplayClass), 1)]
        [InlineData(typeof(DerivedWithBaseDebuggerDisplay), 2)]
        public void GetDebuggerDisplayAttribute_EncompassingTypes(Type type, int? expectedEncompassedTypes)
        {
            // Act
            int? actual = DebuggerDisplayFormatter.GetDebuggerDisplayAttribute(type)?.EncompassingTypes?.Count;

            // Assert
            Assert.Equal(expectedEncompassedTypes, actual);
        }

        [Fact]
        public void GetDebuggerDisplayFormatter_ReturnsWorkingFormatter()
        {
            // Arrange
            DerivedWithBaseDebuggerDisplay testObj = new()
            {
                Count = 10
            };

            FormatterFactoryResult factoryResult = DebuggerDisplayFormatter.GetDebuggerDisplayFormatter(testObj.GetType());
            Assert.NotNull(factoryResult);

            // Act
            string formattedResult = factoryResult.Formatter(testObj);

            // Assert
            Assert.Equal(FormattableString.Invariant($"Count = {testObj.Count}"), formattedResult);
        }
    }
}
