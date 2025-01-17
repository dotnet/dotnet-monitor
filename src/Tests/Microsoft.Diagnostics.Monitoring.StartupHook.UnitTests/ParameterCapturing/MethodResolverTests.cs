// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing;
using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.Boxing;
using Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher.Models;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using SampleMethods;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.UnitTests.ParameterCapturing
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class MethodResolverTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public MethodResolverTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public void ResolveMethodDescription_SkipsMissingModule()
        {
            // Arrange
            MethodResolver resolver = new();
            MethodDescription description = new()
            {
                ModuleName = Guid.NewGuid().ToString("D"),
                TypeName = "Test",
                MethodName = "Test",
            };

            // Act
            List<MethodInfo> methods = resolver.ResolveMethodDescription(description);

            // Assert
            Assert.Empty(methods);
        }

        [Theory]
        // Inheritance
        [InlineData(typeof(TestDerivedSignatures), TestAbstractSignatures.PrivateStaticBaseMethodName, 0)]
        [InlineData(typeof(TestDerivedSignatures), TestAbstractSignatures.PrivateBaseMethodName, 0)]
        [InlineData(typeof(TestDerivedSignatures), TestAbstractSignatures.ProtectedBaseMethodName, 1)]
        [InlineData(typeof(TestAbstractSignatures), TestAbstractSignatures.PrivateStaticBaseMethodName, 1)]
        [InlineData(typeof(TestAbstractSignatures), TestAbstractSignatures.PrivateBaseMethodName, 1)]
        [InlineData(typeof(TestAbstractSignatures), TestAbstractSignatures.ProtectedBaseMethodName, 1)]
        [InlineData(typeof(TestDerivedSignatures), nameof(TestDerivedSignatures.BaseMethod), 1)]
        [InlineData(typeof(TestDerivedSignatures), nameof(TestDerivedSignatures.DerivedMethod), 1)]
        [InlineData(typeof(TestDerivedSignatures), nameof(TestDerivedSignatures.NonInheritedMethod), 1)]

        // Ambiguous
        [InlineData(typeof(StaticTestMethodSignatures), nameof(StaticTestMethodSignatures.AmbiguousMethod), 3)]

        // Special names
        [InlineData(typeof(StaticTestMethodSignatures), "get_" + nameof(StaticTestMethodSignatures.FieldWithGetterAndSetter), 0)]
        public void ResolveMethodDescription_MatchesCorrectly(Type declaringType, string methodName, int matches)
        {
            // Arrange
            MethodResolver resolver = new();
            MethodDescription description = GetMethodDescription(declaringType, methodName);

            // Act
            List<MethodInfo> methods = resolver.ResolveMethodDescription(description);

            // Assert
            Assert.Equal(matches, methods.Count);
        }


        [Fact]
        public void ResolveMethodDescription_Generics_Matches()
        {
            // Arrange
            MethodResolver resolver = new();
            MethodDescription description = new()
            {
                ModuleName = typeof(StaticTestMethodSignatures).Module.Name,
                TypeName = "SampleMethods.GenericTestMethodSignatures`2",
                MethodName = "GenericParameters",
            };

            // Act
            List<MethodInfo> methods = resolver.ResolveMethodDescription(description);

            // Assert
            Assert.Single(methods);
        }

        [Fact]
        public void ResolveMethodDescription_Generics_RequiresArity()
        {
            // Arrange
            MethodResolver resolver = new();
            MethodDescription description = new()
            {
                ModuleName = typeof(StaticTestMethodSignatures).Module.Name,
                TypeName = "SampleMethods.GenericTestMethodSignatures",
                MethodName = "GenericParameters",
            };

            // Act
            List<MethodInfo> methods = resolver.ResolveMethodDescription(description);

            // Assert
            Assert.Empty(methods);
        }

        [Fact]
        public void ResolveMethodDescription_Generics_MatchesAmbiguous()
        {
            // Arrange
            MethodResolver resolver = new();
            MethodDescription description = new()
            {
                ModuleName = typeof(StaticTestMethodSignatures).Module.Name,
                TypeName = "SampleMethods.TestAmbiguousGenericSignatures`1",
                MethodName = "AmbiguousMethod",
            };

            // Act
            List<MethodInfo> methods = resolver.ResolveMethodDescription(description);

            // Assert
            Assert.Equal(2, methods.Count);
        }

        [Fact]
        public void ResolveMethodDescription_CustomAssemblyLoadContext()
        {
            // Arrange
            AssemblyLoadContext customContext = new("Custom context", isCollectible: false);
            // Load an assembly that's already loaded (but not our own assembly as that'll impact other tests in this class).
            Assembly duplicateHostingStartupAssembly = customContext.LoadFromAssemblyPath(typeof(BoxingInstructions).Assembly.Location);
            Assert.NotNull(duplicateHostingStartupAssembly);

            MethodResolver resolver = new();
            MethodDescription description = GetMethodDescription(typeof(BoxingInstructions), nameof(BoxingInstructions.GetBoxingInstructions));

            // Act
            List<MethodInfo> methods = resolver.ResolveMethodDescription(description);

            // Assert
            Assert.Equal(2, methods.Count);

            MethodInfo method1 = methods[0];
            MethodInfo method2 = methods[1];
            Assert.NotEqual(method1.Module.Assembly, method2.Module.Assembly);
            Assert.NotEqual(method1.GetFunctionId(), method2.GetFunctionId());
        }


        private static MethodDescription GetMethodDescription(Type declaringType, string methodName)
        {
            return new MethodDescription
            {
                ModuleName = declaringType.Module.Name,
                TypeName = declaringType.FullName ?? string.Empty,
                MethodName = methodName
            };
        }
    }
}
