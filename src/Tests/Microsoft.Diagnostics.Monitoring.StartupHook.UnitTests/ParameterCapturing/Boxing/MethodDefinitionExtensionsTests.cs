// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.Boxing;
using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.FunctionProbes;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using SampleMethods;
using System;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.UnitTests.ParameterCapturing.Boxing
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class MethodDefinitionExtensionsTests
    {
        [Theory]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.GenericParameters), 2, 2)]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.ValueType_TypeSpec), 6, 17)]
        public void GetParameterBoxingInstructions_Captures_MemoryRegion_ForTypeSpecs(Type declaringType, string methodName, params int[] parameterSignatureLengths)
        {
            MethodInfo? method = declaringType.GetMethod(methodName);
            Assert.NotNull(method);
            TestCore(method, parameterSignatureLengths);
        }

        [Fact]
        public void GetParameterBoxingInstructions_Captures_MemoryRegion_ForTypeGenerics()
        {
            MethodInfo? method = Type.GetType($"{nameof(SampleMethods)}.GenericTestMethodSignatures`2")?.GetMethod("GenericParameters");
            Assert.NotNull(method);
            int[] parameterSignatureLengths = [2, 2, 2];
            TestCore(method, parameterSignatureLengths);
        }

        private static unsafe void TestCore(MethodInfo method, int[] parameterSignatureLengths)
        {
            // Arrange
            Assert.True(method.Module.Assembly.TryGetRawMetadata(out byte* pMdBlob, out int mdLength));

            MetadataReader mdReader = new(pMdBlob, mdLength);

            MethodDefinitionHandle methodDefHandle = (MethodDefinitionHandle)MetadataTokens.Handle(method.MetadataToken);
            MethodDefinition methodDef = mdReader.GetMethodDefinition(methodDefHandle);

            // Act
            ParameterBoxingInstructions[] instructions = methodDef.GetParameterBoxingInstructions(mdReader);

            // Assert
            Assert.Equal(parameterSignatureLengths.Length, instructions.Length);
            for (int i = 0; i < instructions.Length; i++)
            {
                ParameterBoxingInstructions paramInstructions = instructions[i];
                Assert.Equal(InstructionType.TypeSpec, paramInstructions.InstructionType);
                Assert.True(paramInstructions.SignatureBufferPointer != (byte*)0);
                Assert.Equal((uint)parameterSignatureLengths[i], paramInstructions.SignatureBufferLength);
            }
        }
    }
}
