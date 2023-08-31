﻿// Licensed to the .NET Foundation under one or more agreements.
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
    public class MethodTemplateStringGeneratorTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public MethodTemplateStringGeneratorTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Theory]
        [InlineData(typeof(TestMethodSignatures), nameof(TestMethodSignatures.ImplicitThis), "SampleMethods.TestMethodSignatures.ImplicitThis(this: {this})")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.Arrays), "SampleMethods.StaticTestMethodSignatures.Arrays(intArray: {intArray}, multidimensionalArray: {multidimensionalArray})")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.Delegate), "SampleMethods.StaticTestMethodSignatures.Delegate(func: {func})")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.InParam), "SampleMethods.StaticTestMethodSignatures.InParam(in i: <unsupported>)")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.OutParam), "SampleMethods.StaticTestMethodSignatures.OutParam(out i: <unsupported>)")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.RefParam), "SampleMethods.StaticTestMethodSignatures.RefParam(ref i: <unsupported>)")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.RefStruct), "SampleMethods.StaticTestMethodSignatures.RefStruct(ref myRefStruct: <unsupported>)")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.GenericParameters), "SampleMethods.StaticTestMethodSignatures.GenericParameters<T, K>(t: <unsupported>, k: <unsupported>)")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.VarArgs), "SampleMethods.StaticTestMethodSignatures.VarArgs(b: {b}, myInts: {myInts})")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.Unicode_ΦΨ), "SampleMethods.StaticTestMethodSignatures.Unicode_ΦΨ(δ: {δ})")]
        [InlineData(typeof(StaticTestMethodSignatures.SampleNestedStruct), nameof(StaticTestMethodSignatures.SampleNestedStruct.DoWork), "SampleMethods.StaticTestMethodSignatures+SampleNestedStruct.DoWork(this: <unsupported>, i: {i})")]
        public void MethodTemplateString(Type declaringType, string methodName, string templateString)
        {
            // Arrange
            MethodInfo method = declaringType.GetMethod(methodName);
            Assert.NotNull(method);

            // Act
            bool[] supportedParameters = BoxingTokens.AreParametersSupported(BoxingTokens.GetBoxingTokens(method));
            string actualTemplateString = MethodTemplateStringGenerator.GenerateTemplateString(method, supportedParameters);

            // Assert
            Assert.NotNull(actualTemplateString);
            actualTemplateString = actualTemplateString.ReplaceLineEndings("").Replace("\t", "");
            _outputHelper.WriteLine(actualTemplateString);
            Assert.Equal(templateString, actualTemplateString);
        }
    }
}
