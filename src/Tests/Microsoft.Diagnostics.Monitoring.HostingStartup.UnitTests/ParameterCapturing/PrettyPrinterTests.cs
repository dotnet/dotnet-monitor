// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using System.Reflection;
using System;
using Xunit;
using SampleMethods;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.UnitTests.ParameterCapturing
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class PrettyPrinterTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public PrettyPrinterTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Theory]
        [InlineData(typeof(TestMethodSignatures), nameof(TestMethodSignatures.ImplicitThis), "SampleMethods.TestMethodSignatures.ImplicitThis(this: {0})")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.Delegate), "SampleMethods.StaticTestMethodSignatures.Delegate(func: {0})")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.BasicTypes), "SampleMethods.StaticTestMethodSignatures.BasicTypes(s: {0}, intArray: {1}, multidimensionalArray: {2}, uInt: {3})")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.InParam), "SampleMethods.StaticTestMethodSignatures.InParam(in i: {{unsupported}})")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.OutParam), "SampleMethods.StaticTestMethodSignatures.OutParam(out i: {{unsupported}})")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.RefParam), "SampleMethods.StaticTestMethodSignatures.RefParam(ref i: {{unsupported}})")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.RefStruct), "SampleMethods.StaticTestMethodSignatures.RefStruct(ref myRefStruct: {{unsupported}})")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.GenericParameters), "SampleMethods.StaticTestMethodSignatures.GenericParameters<T, K>(t: {{unsupported}}, k: {{unsupported}})")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.VarArgs), "SampleMethods.StaticTestMethodSignatures.VarArgs(b: {0}, myInts: {1})")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.Unicode_ΦΨ), "SampleMethods.StaticTestMethodSignatures.Unicode_ΦΨ(δ: {0})")]
        [InlineData(typeof(StaticTestMethodSignatures.SampleNestedStruct), nameof(StaticTestMethodSignatures.SampleNestedStruct.DoWork), "SampleMethods.StaticTestMethodSignatures+SampleNestedStruct.DoWork(this: {{unsupported}}, i: {0})")]
        public void MethodFormattableString(Type containingClassType, string methodName, string formattableString)
        {
            // Arrange
            MethodInfo method = containingClassType.GetMethod(methodName);
            Assert.NotNull(method);

            // Act
            bool[] supportedParameters = BoxingTokens.AreParametersSupported(BoxingTokens.GetBoxingTokens(method));
            string actualFormattableString = PrettyPrinter.ConstructFormattableStringFromMethod(method, supportedParameters);

            // Assert
            Assert.NotNull(actualFormattableString);
            actualFormattableString = actualFormattableString.ReplaceLineEndings("").Replace("\t", "");
            _outputHelper.WriteLine(actualFormattableString);
            Assert.Equal(formattableString, actualFormattableString);
        }

        [Theory]
        [InlineData(null, "null")]
        [InlineData("test", "'test'")]
        [InlineData(5, "5")]
        [InlineData(MyEnum.ValueA, nameof(MyEnum.ValueA))]
        public void FormatObject(object obj, string value)
        {
            // Act
            string actualValue = PrettyPrinter.FormatObject(obj);

            // Assert
            Assert.Equal(value, actualValue);
        }
    }
}
