// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using SampleMethods;
using System;
using System.Reflection;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.UnitTests.ParameterCapturing
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class MethodTemplateStringTests
    {
        [Theory]
        [InlineData(typeof(TestMethodSignatures), nameof(TestMethodSignatures.ImplicitThis), "SampleMethods.TestMethodSignatures.ImplicitThis(this: {this})")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.Arrays), "SampleMethods.StaticTestMethodSignatures.Arrays(intArray: {intArray}, multidimensionalArray: {multidimensionalArray})")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.Delegate), "SampleMethods.StaticTestMethodSignatures.Delegate(func: {func})")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.InParam), "SampleMethods.StaticTestMethodSignatures.InParam(in i: {i})")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.OutParam), "SampleMethods.StaticTestMethodSignatures.OutParam(out i: {i})")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.RefParam), "SampleMethods.StaticTestMethodSignatures.RefParam(ref i: {i})")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.RefStruct), "SampleMethods.StaticTestMethodSignatures.RefStruct(ref myRefStruct: {myRefStruct})")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.GenericParameters), "SampleMethods.StaticTestMethodSignatures.GenericParameters<T, K>(t: {t}, k: {k})")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.VarArgs), "SampleMethods.StaticTestMethodSignatures.VarArgs(b: {b}, myInts: {myInts})")]
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.Unicode_ΦΨ), "SampleMethods.StaticTestMethodSignatures.Unicode_ΦΨ(δ: {δ})")]
        [InlineData(typeof(StaticTestMethodSignatures.SampleNestedStruct), nameof(StaticTestMethodSignatures.SampleNestedStruct.DoWork), "SampleMethods.StaticTestMethodSignatures+SampleNestedStruct.DoWork(this: {this}, i: {i})")]
        public void MethodTemplateString(Type declaringType, string methodName, string templateString)
        {
            // Arrange
            MethodInfo method = declaringType.GetMethod(methodName);
            Assert.NotNull(method);

            // Act
            string actualTemplateString = new MethodTemplateString(method).Template;

            // Assert
            Assert.NotNull(actualTemplateString);
            actualTemplateString = actualTemplateString.ReplaceLineEndings("").Replace("\t", "");
            Assert.Equal(templateString, actualTemplateString);
        }
    }
}
