// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.ObjectFormatting;
using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.ObjectFormatting.Formatters.DebuggerDisplay;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.Diagnostics;
using Xunit;
using static Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.ObjectFormatting.Formatters.DebuggerDisplay.DebuggerDisplayFormatter;
using static Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing.ParameterCapturingEvents;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.UnitTests.ParameterCapturing.ObjectFormatting.Formatters.DebuggerDisplay
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

        [DebuggerDisplay("Count = {Count} NullField = {NullField}")]
        private sealed class DebuggerDisplayClassWithNullField
        {
            public int Count { get; set; }

#pragma warning disable CS8603 // Possible null reference return.
            public static string NullField => null;
#pragma warning restore CS8603 // Possible null reference return.
        }

        [Theory]
        [InlineData(typeof(NoDebuggerDisplay), null)]
        [InlineData(typeof(DebuggerDisplayClass), "Count = {Count}")]
        [InlineData(typeof(DerivedWithBaseDebuggerDisplay), "Count = {Count}")]
        public void GetDebuggerDisplayAttribute(Type type, string? expected)
        {
            // Act
            DebuggerDisplayAttributeValue? attribute = DebuggerDisplayFormatter.GetDebuggerDisplayAttribute(type);

            // Assert
            if (expected == null)
            {
                Assert.Null(attribute);
                return;
            }

            Assert.NotNull(attribute);
            Assert.Equal(expected, attribute.Value.Text);
        }

        [Theory]
        [InlineData(typeof(NoDebuggerDisplay), null)]
        [InlineData(typeof(DebuggerDisplayClass), 1)]
        [InlineData(typeof(DerivedWithBaseDebuggerDisplay), 2)]
        public void GetDebuggerDisplayAttribute_EncompassingTypes(Type type, int? expectedEncompassedTypes)
        {
            // Act
            DebuggerDisplayAttributeValue? attribute = DebuggerDisplayFormatter.GetDebuggerDisplayAttribute(type);

            // Assert
            if (expectedEncompassedTypes == null)
            {
                Assert.Null(attribute);
                return;
            }

            Assert.NotNull(attribute);
            Assert.Equal(expectedEncompassedTypes, attribute.Value.EncompassingTypes.Count);
        }

        [Fact]
        public void GetDebuggerDisplayFormatter_ReturnsWorkingFormatter()
        {
            // Arrange
            DerivedWithBaseDebuggerDisplay testObj = new()
            {
                Count = 10
            };

            FormatterFactoryResult? factoryResult = DebuggerDisplayFormatter.GetDebuggerDisplayFormatter(testObj.GetType());
            Assert.NotNull(factoryResult);

            // Act
            ObjectFormatterResult formattedResult = factoryResult.Formatter(testObj);

            // Assert
            Assert.Equal(FormattableString.Invariant($"'Count = {testObj.Count}'"), formattedResult.FormattedValue);
            Assert.Equal(ParameterEvaluationResult.Success, formattedResult.EvalResult);
        }

        [Fact]
        public void GetDebuggerDisplayFormatter_FormatsNull()
        {
            // Arrange
            DebuggerDisplayClassWithNullField testObj = new()
            {
                Count = 10
            };

            FormatterFactoryResult? factoryResult = DebuggerDisplayFormatter.GetDebuggerDisplayFormatter(testObj.GetType());
            Assert.NotNull(factoryResult);

            // Act
            ObjectFormatterResult formattedResult = factoryResult.Formatter(testObj);

            // Assert
            Assert.Equal(FormattableString.Invariant($"'Count = {testObj.Count} NullField = null'"), formattedResult.FormattedValue);
            Assert.Equal(ParameterEvaluationResult.Success, formattedResult.EvalResult);
        }
    }
}
