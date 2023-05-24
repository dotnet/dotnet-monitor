// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using SampleMethods;
using System;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.UnitTests.ParameterCapturing
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class BoxingTokensTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public BoxingTokensTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Theory]
        [InlineData(typeof(TestMethodSignatures), nameof(TestMethodSignatures.ImplicitThis), true)]
        [InlineData(typeof(StaticTestMethodSignatures.SampleNestedStruct), nameof(StaticTestMethodSignatures.SampleNestedStruct.DoWork), false, true)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.NoArgs))]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.BasicTypes), true, true, true, true)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.Delegate), true)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.ExplicitThis), true)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.InParam), false)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.RefParam), false)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.OutParam), false)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.RefStruct), false)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.RecordStruct), true)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.Pointer), false)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.GenericParameters), false, false)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.TypeDef), true)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.TypeRef), true)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.TypeSpec), true)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.ValueType_TypeDef), true)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.ValueType_TypeRef), false)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.ValueType_TypeSpec), false)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.VarArgs), true, true)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.Unicode_ΦΨ), true)]
        public void GetBoxingTokens_HandlesUnsupportedParameters(Type containingClassType, string methodName, params bool[] supported)
        {
            // Arrange
            MethodInfo method = containingClassType.GetMethod(methodName);

            // Act
            bool[] supportedParameters = BoxingTokens.AreParametersSupported(BoxingTokens.GetBoxingTokens(method));

            // Assert
            Assert.Equal(supported, supportedParameters);
        }

        [Fact]
        public void GetBoxingTokens_HandlesUnsupportedParameters_Generics()
        {
            // Arrange
            MethodInfo method = Type.GetType($"{nameof(SampleMethods)}.GenericTestMethodSignatures`2").GetMethod("GenericParameters");
            bool[] supported = new bool[] { true, false, false, false };

            // Act
            bool[] supportedParameters = BoxingTokens.AreParametersSupported(BoxingTokens.GetBoxingTokens(method));

            // Assert
            Assert.Equal(supported, supportedParameters);
        }
    }
}
