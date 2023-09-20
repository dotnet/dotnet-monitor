﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.ObjectFormatting;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using SampleMethods;
using System;
using System.Reflection;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.UnitTests.ParameterCapturing
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class ObjectFormatterFactoryTests
    {
        [Theory]
        [InlineData(typeof(string), nameof(RuntimeFormatters.IConvertibleFormatter))]
        [InlineData(typeof(MyEnum), nameof(RuntimeFormatters.IConvertibleFormatter))]
        [InlineData(typeof(FormattableString), nameof(RuntimeFormatters.IFormattableFormatter))]
        [InlineData(typeof(object), nameof(RuntimeFormatters.GeneralFormatter))]
        public void GetFormatter_ReturnsCorrectFormatter(Type type, string expectedFormatterName)
        {
            // Act
            ObjectFormatterFunc formatter = ObjectFormatterFactory.GetFormatter(type).Formatter;

            // Assert
            Assert.Equal(expectedFormatterName, formatter.GetMethodInfo().Name);
        }
    }
}
