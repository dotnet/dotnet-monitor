// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.Boxing;
using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.FunctionProbes;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using SampleMethods;
using System;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.UnitTests.ParameterCapturing.Boxing
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class BoxingInstructionsTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public BoxingInstructionsTests(ITestOutputHelper outputHelper)
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
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.NativeIntegers), true, true)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.Pointer), false)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.GenericParameters), true, true)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.TypeDef), true)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.TypeRef), true)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.TypeSpec), true)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.ValueType_TypeDef), true)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.ValueType_TypeRef), true)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.ValueType_TypeSpec), true, true)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.VarArgs), true, true)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.Unicode_ΦΨ), true)]
        public void GetBoxingInstructions_Detects_UnsupportedParameters(Type declaringType, string methodName, params bool[] supported)
        {
            // Arrange
            MethodInfo? method = declaringType.GetMethod(methodName);
            Assert.NotNull(method);

            // Act
            bool[] supportedParameters = BoxingInstructions.AreParametersSupported(BoxingInstructions.GetBoxingInstructions(method));

            // Assert
            Assert.Equal(supported, supportedParameters);
        }

        [Fact]
        public void GetBoxingInstructions_Handles_GenericParameters()
        {
            // Arrange
            MethodInfo? method = Type.GetType($"{nameof(SampleMethods)}.GenericTestMethodSignatures`2")?.GetMethod("GenericParameters");
            Assert.NotNull(method);
            bool[] supported = new bool[] { true, true, true, true };

            // Act
            bool[] supportedParameters = BoxingInstructions.AreParametersSupported(BoxingInstructions.GetBoxingInstructions(method));

            // Assert
            Assert.Equal(supported, supportedParameters);
        }

        [Fact]
        public void GetBoxingInstructions_Handles_Primitives()
        {
            // Arrange
            ParameterBoxingInstructions[] expectedInstructions = [
                SpecialCaseBoxingTypes.Boolean,
                SpecialCaseBoxingTypes.Char,
                SpecialCaseBoxingTypes.SByte,
                SpecialCaseBoxingTypes.Byte,
                SpecialCaseBoxingTypes.Int16,
                SpecialCaseBoxingTypes.UInt16,
                SpecialCaseBoxingTypes.Int32,
                SpecialCaseBoxingTypes.UInt32,
                SpecialCaseBoxingTypes.Int64,
                SpecialCaseBoxingTypes.UInt64,
                SpecialCaseBoxingTypes.Single,
                SpecialCaseBoxingTypes.Double,
            ];
            MethodInfo? method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.Primitives));
            Assert.NotNull(method);

            // Act
            ParameterBoxingInstructions[] actualInstructions = BoxingInstructions.GetBoxingInstructions(method);

            // Assert
            Assert.Equal(expectedInstructions, actualInstructions);
        }

        [Fact]
        public void GetBoxingInstructions_Handles_BuiltInReferenceTypes()
        {
            // Arrange
            ParameterBoxingInstructions[] expectedInstructions = [
                SpecialCaseBoxingTypes.Object,
                SpecialCaseBoxingTypes.Object,
                SpecialCaseBoxingTypes.Object,
            ];
            MethodInfo? method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.BuiltInReferenceTypes));
            Assert.NotNull(method);

            // Act
            ParameterBoxingInstructions[] actualInstructions = BoxingInstructions.GetBoxingInstructions(method);

            // Assert
            Assert.Equal(expectedInstructions, actualInstructions);
        }

        [Theory]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.Primitives))]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.Arrays))]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.BuiltInReferenceTypes))]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.Delegate))]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.ExplicitThis))]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.InParam))]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.RefParam))]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.OutParam))]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.RefStruct))]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.RecordStruct))]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.NativeIntegers))]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.Pointer))]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.GenericParameters))]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.TypeDef))]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.TypeRef))]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.TypeSpec))]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.ValueType_TypeDef))]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.ValueType_TypeRef))]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.ValueType_TypeSpec))]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.VarArgs))]
        public void ReflectionAndSignatureDecoder_Contract_InSync(Type declaringType, string methodName)
        {
            MethodInfo? method = declaringType.GetMethod(methodName);
            ReflectionAndSignatureDecoder_Contract_InSyncCore(method);
        }

        [Fact]
        public void ReflectionAndSignatureDecoder_Contract_Generics_InSync()
        {
            MethodInfo? method = Type.GetType($"{nameof(SampleMethods)}.GenericTestMethodSignatures`2")?.GetMethod("GenericParameters");
            ReflectionAndSignatureDecoder_Contract_InSyncCore(method);
        }

        /// <summary>
        /// Tests if GetBoxingInstructionsFromReflection is in sync with the signature decoder support for a given method's parameters.
        /// </summary>
        /// <param name="method">The method whose parameters to test.</param>
        private static void ReflectionAndSignatureDecoder_Contract_InSyncCore(MethodInfo? method)
        {
            Assert.NotNull(method);

            ParameterInfo[] parameters = method.GetParameters();

            ParameterBoxingInstructions[]? signatureDecoderInstructions = BoxingInstructions.GetAncillaryBoxingInstructionsFromMethodSignature(method);
            Assert.NotNull(signatureDecoderInstructions);
            Assert.Equal(parameters.Length, signatureDecoderInstructions.Length);

            for (int i = 0; i < parameters.Length; i++)
            {
                //
                // If GetBoxingInstructionsFromReflection sets canUseSignatureDecoder then the following must be true:
                // - GetBoxingInstructionsFromReflection was unable to get boxing instructions for the parameter.
                // - The signature decoder is able to get boxing instructions for the parameter.
                //
                //
                // NOTE: The signature decoder may produce a superset of boxing instructions compared to what GetBoxingInstructionsFromReflection needs.
                // This is okay as GetBoxingInstructionsFromReflection determines when to leverage the signature decoder.
                //
                ParameterBoxingInstructions reflectionInstructions = BoxingInstructions.GetBoxingInstructionsFromReflection(method, parameters[i].ParameterType, out bool canUseSignatureDecoder);
                if (canUseSignatureDecoder)
                {
                    Assert.False(BoxingInstructions.IsParameterSupported(reflectionInstructions));
                    Assert.True(BoxingInstructions.IsParameterSupported(signatureDecoderInstructions[i]));
                }
            }
        }
    }
}
