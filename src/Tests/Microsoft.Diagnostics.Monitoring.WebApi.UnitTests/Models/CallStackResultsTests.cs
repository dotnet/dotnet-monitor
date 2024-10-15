// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.WebApi.UnitTests.Models
{
    public class CallStackResultsTests
    {
        [Theory]
        [InlineData("Test", "Test")]
        [InlineData("Test[System.String]", "Test", "System.String")]
        [InlineData("Test[System.String,System.Object]", "Test", "System.String", "System.Object")]
        public void MethodNameWithGenericArgTypes_Get(string expectedName, string methodName, params string[] genericArgTypes)
        {
            CallStackFrame frame = new()
            {
                MethodName = methodName,
                FullGenericArgTypes = genericArgTypes
            };

            Assert.Equal(expectedName, frame.MethodNameWithGenericArgTypes);
        }

        [Theory]
        [InlineData("Test", "Test")]
        [InlineData("[NativeFrame]", "[NativeFrame]")]
        [InlineData("Test[System.String]", "Test", "System.String")]
        [InlineData("Test[System.String,System.Object]", "Test", "System.String", "System.Object")]
        public void MethodNameWithGenericArgTypes_Set(string serializedName, string expectedMethodName, params string[] expectedGenericArgTypes)
        {
            CallStackFrame frame = new()
            {
                MethodNameWithGenericArgTypes = serializedName
            };

            Assert.Equal(expectedMethodName, frame.MethodName);
            Assert.Equal(expectedGenericArgTypes, frame.FullGenericArgTypes);
        }

        [Theory]
        [InlineData("Test[")]
        [InlineData("Test[System.String,System.Object")]
        public void MethodNameWithGenericArgTypes_Set_InvalidInput_Throws(string serializedName)
        {
            CallStackFrame frame = new();
            Assert.Throws<InvalidOperationException>(() => frame.MethodNameWithGenericArgTypes = serializedName);
        }
    }
}
