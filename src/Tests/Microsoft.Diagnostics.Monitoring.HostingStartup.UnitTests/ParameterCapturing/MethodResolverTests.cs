// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing;
using Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher.Models;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using SampleMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.UnitTests.ParameterCapturing
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
                AssemblyName = Guid.NewGuid().ToString("D"),
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
                AssemblyName = typeof(StaticTestMethodSignatures).Assembly.GetName().Name,
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
                AssemblyName = typeof(StaticTestMethodSignatures).Assembly.GetName().Name,
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
                AssemblyName = typeof(StaticTestMethodSignatures).Assembly.GetName().Name,
                TypeName = "SampleMethods.TestAmbiguousGenericSignatures`1",
                MethodName = "AmbiguousMethod",
            };

            // Act
            List<MethodInfo> methods = resolver.ResolveMethodDescription(description);

            // Assert
            Assert.Equal(2, methods.Count);
        }

        [Fact]
        public void DoesResolve_CustomAssemblyLoadContext()
        {
            // Arrange
            AssemblyLoadContext customContext = new("Custom context", isCollectible: true);
            Assembly loadedAssembly = customContext.LoadFromAssemblyPath(@"C:\Users\joschmit\work\buggy-demo-code\src\BuggyDemoWeb\bin\Release\net6.0\BuggyDemoWeb.dll");
            Assert.NotNull(loadedAssembly);

            MethodResolver resolver = new();
            MethodDescription description = new()
            {
                AssemblyName = loadedAssembly.GetName().Name,
                TypeName = "BuggyDemoWeb.Program",
                MethodName = "Main",
            };
            // Act
            List<MethodInfo> methods = resolver.ResolveMethodDescription(description);

            // Assert
            Assert.Single(methods);
        }


        private static MethodDescription GetMethodDescription(Type declaringType, string methodName)
        {
            return new MethodDescription
            {
                AssemblyName = declaringType.Assembly.GetName().Name,
                TypeName = declaringType.FullName,
                MethodName = methodName
            };
        }
    }
}
