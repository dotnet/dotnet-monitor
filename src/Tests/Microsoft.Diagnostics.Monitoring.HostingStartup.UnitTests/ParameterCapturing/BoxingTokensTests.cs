// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using SampleMethods;
using System;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.BoxingTokens;

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
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.Arrays), true, true)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.BuiltInReferenceTypes), true, true, true)]
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
        public void GetBoxingTokens_Detects_UnsupportedParameters(Type containingClassType, string methodName, params bool[] supported)
        {
            // Arrange
            MethodInfo method = containingClassType.GetMethod(methodName);

            // Act
            bool[] supportedParameters = BoxingTokens.AreParametersSupported(BoxingTokens.GetBoxingTokens(method));

            // Assert
            Assert.Equal(supported, supportedParameters);
        }

        [Fact]
        public void GetBoxingTokens_Detects_UnsupportedGenericParameters()
        {
            // Arrange
            MethodInfo method = Type.GetType($"{nameof(SampleMethods)}.GenericTestMethodSignatures`2").GetMethod("GenericParameters");
            bool[] supported = new bool[] { true, false, false, false };

            // Act
            bool[] supportedParameters = BoxingTokens.AreParametersSupported(BoxingTokens.GetBoxingTokens(method));

            // Assert
            Assert.Equal(supported, supportedParameters);
        }

        [Fact]
        public void GetBoxingTokens_Handles_Primitives()
        {
            // Arrange
            uint[] expectedBoxingTokens = new uint[] {
                SpecialCaseBoxingTypes.Boolean.BoxingToken(),
                SpecialCaseBoxingTypes.Char.BoxingToken(),
                SpecialCaseBoxingTypes.SByte.BoxingToken(),
                SpecialCaseBoxingTypes.Byte.BoxingToken(),
                SpecialCaseBoxingTypes.Int16.BoxingToken(),
                SpecialCaseBoxingTypes.UInt16.BoxingToken(),
                SpecialCaseBoxingTypes.Int32.BoxingToken(),
                SpecialCaseBoxingTypes.UInt32.BoxingToken(),
                SpecialCaseBoxingTypes.Int64.BoxingToken(),
                SpecialCaseBoxingTypes.UInt64.BoxingToken(),
                SpecialCaseBoxingTypes.Single.BoxingToken(),
                SpecialCaseBoxingTypes.Double.BoxingToken()
            };
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.Primitives));

            // Act
            uint[] actualBoxingTokens = BoxingTokens.GetBoxingTokens(method);

            // Assert
            Assert.Equal(expectedBoxingTokens, actualBoxingTokens);
        }

        [Fact]
        public void GetBoxingTokens_Handles_BuiltInReferenceTypes()
        {
            // Arrange
            uint[] expectedBoxingTokens = new uint[] {
                SpecialCaseBoxingTypes.Object.BoxingToken(),
                SpecialCaseBoxingTypes.Object.BoxingToken(),
                SpecialCaseBoxingTypes.Object.BoxingToken()
            };
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.BuiltInReferenceTypes));

            // Act
            uint[] actualBoxingTokens = BoxingTokens.GetBoxingTokens(method);

            // Assert
            Assert.Equal(expectedBoxingTokens, actualBoxingTokens);
        }
    }
}
