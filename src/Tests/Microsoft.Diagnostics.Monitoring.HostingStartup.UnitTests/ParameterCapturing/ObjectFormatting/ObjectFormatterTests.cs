// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.ObjectFormatting;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.UnitTests.ParameterCapturing
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
            string actual = ObjectFormatter.FormatObject(formatter, 5);

            // Assert
            Assert.Equal(ObjectFormatter.Tokens.Exception, actual);
        }

        [Fact]
        public void FormatObject_Handles_Null()
        {
            // Arrange
            ObjectFormatterFunc formatter = (object obj, FormatSpecifier _) =>
            {
                return string.Empty;
            };

            // Act
            string actual = ObjectFormatter.FormatObject(formatter, null);

            // Assert
            Assert.Equal(ObjectFormatter.Tokens.Null, actual);
        }

        [Fact]
        public void FormatObject_Passes_FormatSpecifier()
        {
            // Arrange
            FormatSpecifier? actualSpecifier = null;
            ObjectFormatterFunc formatter = (object obj, FormatSpecifier specifier) =>
            {
                actualSpecifier = specifier;
                return string.Empty;
            };

            // Act
            string actual = ObjectFormatter.FormatObject(formatter, 10, FormatSpecifier.NoQuotes);

            // Assert
            Assert.Equal(FormatSpecifier.NoQuotes, actualSpecifier);
        }
    }
}
