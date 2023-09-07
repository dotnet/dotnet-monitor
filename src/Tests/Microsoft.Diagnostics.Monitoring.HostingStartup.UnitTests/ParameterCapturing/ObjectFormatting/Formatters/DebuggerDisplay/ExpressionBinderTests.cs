﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.ObjectFormatting.Formatters.DebuggerDisplay;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using Xunit;
using static Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.ObjectFormatting.Formatters.DebuggerDisplay.ExpressionBinder;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.UnitTests.ParameterCapturing.ObjectFormatting.Formatters.DebuggerDisplay
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class ExpressionBinderTests
    {
        private sealed class DebuggerDisplayClass
        {
            public int Field = 10;
            public static Uri StaticProperty { get; set; } = new Uri("http://www.bing.com/static");
            public int Count { get; set; } = 10;

            public Uri MyUri { get; }

            public DebuggerDisplayClass(string uri)
            {
                RecursionProp = this;
                MyUri = new Uri(uri);
            }

            public DebuggerDisplayClass RecursionProp { get; }
            public DebuggerDisplayClass Recursion() => this;

            public static void WithArgs(int i) { }

            public string GetCountAsString()
            {
                return Count.ToString();
            }

            public void NoReturnType() => Count++;
        }

        [Theory]
        [InlineData("GetCountAsString()", true, "10")]
        [InlineData("DoesntExist()", false, null)]
        [InlineData("WithArgs(Count)", false, null)]
        [InlineData("Count", true, 10)]
        [InlineData("Field", true, 10)]
        [InlineData("Incomplete.Chain.", false, null)]
        [InlineData("NoReturnType()", true, null)]
        // Chained expression with implicit this type change
        [InlineData("Recursion().RecursionProp.MyUri.Host", true, "www.bing.com")]
        // Chained expression with static property
        [InlineData("Recursion().StaticProperty.Host", true, "www.bing.com")]
        public void BindExpression(string expression, bool doesBind, object expected)
        {
            // Arrange
            DebuggerDisplayClass obj = new("https://www.bing.com/abc");

            // Act
            ExpressionEvaluator evaluator = ExpressionBinder.BindExpression(obj.GetType(), expression);
            object result = evaluator?.Evaluate(obj);

            // Assert
            if (!doesBind)
            {
                Assert.Null(evaluator);
                return;
            }

            Assert.NotNull(evaluator);
            Assert.Equal(expected, result);
        }
    }
}
