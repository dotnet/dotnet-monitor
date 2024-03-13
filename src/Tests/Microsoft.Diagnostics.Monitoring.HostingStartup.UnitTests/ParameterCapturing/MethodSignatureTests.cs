// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using SampleMethods;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.UnitTests.ParameterCapturing
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class MethodSignatureTests
    {
        [Theory]
        [MemberData(nameof(GetMethodSignatureTestData))]
        public void MethodSignature(
            MethodInfo methodInfo,
            string expectedMethodName,
            string expectedDeclaringType,
            string expectedDeclaringTypeModuleName,
            ExpectedParameterSignature[] expectedParameters)
        {
            // Arrange
            Assert.NotNull(methodInfo);

            // Act
            MethodSignature methodSignature = new MethodSignature(methodInfo);

            // Assert
            Assert.NotNull(methodSignature);
            Assert.Equal(expectedMethodName, methodSignature.MethodName);
            Assert.Equal(expectedDeclaringType, methodSignature.TypeName);
            Assert.Equal(expectedDeclaringTypeModuleName, methodSignature.ModuleName);
            Assert.Equal(expectedParameters.Length, methodSignature.Parameters.Count);
            for (var idx = 0; idx < expectedParameters.Length; idx++)
            {
                Assert.Equal(expectedParameters[idx].Name, methodSignature.Parameters[idx].Name);
                Assert.Equal(expectedParameters[idx].Type, methodSignature.Parameters[idx].Type);
                Assert.Equal(expectedParameters[idx].TypeModuleName, methodSignature.Parameters[idx].TypeModuleName);
                Assert.Equal(expectedParameters[idx].Attributes, methodSignature.Parameters[idx].Attributes);
                Assert.Equal(expectedParameters[idx].IsByRef, methodSignature.Parameters[idx].IsByRef);
            }
        }

        public static IEnumerable<object[]> GetMethodSignatureTestData()
        {
            yield return new object[]
            {
                typeof(TestMethodSignatures).GetMethod(nameof(TestMethodSignatures.ImplicitThis)),
                "ImplicitThis",
                "SampleMethods.TestMethodSignatures",
                typeof(TestMethodSignatures).Module.Name,
                new ExpectedParameterSignature[]
                {
                    new(typeof(TestMethodSignatures))
                    {
                        Name = "this",
                        Type = "SampleMethods.TestMethodSignatures",
                        Attributes = ParameterAttributes.None,
                        IsByRef = false
                    }
                }
            };
            yield return new object[]
            {
                typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.Arrays)),
                "Arrays",
                "SampleMethods.StaticTestMethodSignatures",
                typeof(StaticTestMethodSignatures).Module.Name,
                new ExpectedParameterSignature[]
                {
                    new(typeof(int[]))
                    {
                        Name = "intArray",
                        Type = "System.Int32[]",
                        Attributes = ParameterAttributes.None,
                        IsByRef = false
                    },
                    new(typeof(bool[,]))
                    {
                        Name = "multidimensionalArray",
                        Type = "System.Boolean[,]",
                        Attributes = ParameterAttributes.None,
                        IsByRef = false
                    }
                }
            };
            yield return new object[]
            {
                typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.Delegate)),
                "Delegate",
                "SampleMethods.StaticTestMethodSignatures",
                typeof(StaticTestMethodSignatures).Module.Name,
                new ExpectedParameterSignature[]
                {
                    new(typeof(StaticTestMethodSignatures.MyDelegate))
                    {
                        Name = "func",
                        Type = "SampleMethods.StaticTestMethodSignatures+MyDelegate",
                        Attributes = ParameterAttributes.None,
                        IsByRef = false
                    },
                }
            };
            yield return new object[]
            {
                typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.InParam)),
                "InParam",
                "SampleMethods.StaticTestMethodSignatures",
                typeof(StaticTestMethodSignatures).Module.Name,
                new ExpectedParameterSignature[]
                {
                    new(typeof(int))
                    {
                        Name = "i",
                        Type = "System.Int32&",
                        Attributes = ParameterAttributes.In,
                        IsByRef = true
                    },
                }
            };
            yield return new object[]
            {
                typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.OutParam)),
                "OutParam",
                "SampleMethods.StaticTestMethodSignatures",
                typeof(StaticTestMethodSignatures).Module.Name,
                new ExpectedParameterSignature[]
                {
                    new(typeof(int))
                    {
                        Name = "i",
                        Type = "System.Int32&",
                        Attributes = ParameterAttributes.Out,
                        IsByRef = true
                    },
                }
            };
            yield return new object[]
            {
                typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.RefParam)),
                "RefParam",
                "SampleMethods.StaticTestMethodSignatures",
                typeof(StaticTestMethodSignatures).Module.Name,
                new ExpectedParameterSignature[]
                {
                    new(typeof(int))
                    {
                        Name = "i",
                        Type = "System.Int32&",
                        Attributes = ParameterAttributes.None,
                        IsByRef = true
                    },
                }
            };
            yield return new object[]
            {
                typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.RefStruct)),
                "RefStruct",
                "SampleMethods.StaticTestMethodSignatures",
                typeof(StaticTestMethodSignatures).Module.Name,
                new ExpectedParameterSignature[]
                {
                    new(typeof(MyRefStruct))
                    {
                        Name = "myRefStruct",
                        Type = "SampleMethods.MyRefStruct&",
                        Attributes = ParameterAttributes.None,
                        IsByRef = true
                    },
                }
            };
            yield return new object[]
            {
                typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.GenericParameters)),
                "GenericParameters<T, K>",
                "SampleMethods.StaticTestMethodSignatures",
                typeof(StaticTestMethodSignatures).Module.Name,
                new ExpectedParameterSignature[]
                {
                    new(typeof(MethodSignatureTests))
                    {
                        Name = "t",
                        Type = null,
                        Attributes = ParameterAttributes.None,
                        IsByRef = false
                    },
                    new(typeof(MethodSignatureTests))
                    {
                        Name = "k",
                        Type = null,
                        Attributes = ParameterAttributes.None,
                        IsByRef = false
                    },
                }
            };
            yield return new object[]
            {
                typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.VarArgs)),
                "VarArgs",
                "SampleMethods.StaticTestMethodSignatures",
                typeof(StaticTestMethodSignatures).Module.Name,
                new ExpectedParameterSignature[]
                {
                    new(typeof(bool))
                    {
                        Name = "b",
                        Type = "System.Boolean",
                        Attributes = ParameterAttributes.None,
                        IsByRef = false
                    },
                    new(typeof(int[]))
                    {
                        Name = "myInts",
                        Type = "System.Int32[]",
                        Attributes = ParameterAttributes.None,
                        IsByRef = false
                    },
                }
            };
            yield return new object[]
            {
                typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.Unicode_ΦΨ)),
                "Unicode_ΦΨ",
                "SampleMethods.StaticTestMethodSignatures",
                typeof(StaticTestMethodSignatures).Module.Name,
                new ExpectedParameterSignature[]
                {
                    new(typeof(bool))
                    {
                        Name = "δ",
                        Type = "System.Boolean",
                        Attributes = ParameterAttributes.None,
                        IsByRef = false
                    },
                }
            };
            yield return new object[]
            {
                typeof(StaticTestMethodSignatures.SampleNestedStruct).GetMethod(nameof(StaticTestMethodSignatures.SampleNestedStruct.DoWork)),
                "DoWork",
                "SampleMethods.StaticTestMethodSignatures+SampleNestedStruct",
                typeof(StaticTestMethodSignatures).Module.Name,
                new ExpectedParameterSignature[]
                {
                    new(typeof(StaticTestMethodSignatures.SampleNestedStruct))
                    {
                        Name = "this",
                        Type = "SampleMethods.StaticTestMethodSignatures+SampleNestedStruct",
                        Attributes = ParameterAttributes.None,
                        IsByRef = false
                    },
                    new(typeof(int))
                    {
                        Name = "i",
                        Type = "System.Int32",
                        Attributes = ParameterAttributes.None,
                        IsByRef = false
                    },
                }
            };
        }

        public sealed record ExpectedParameterSignature(Type paramType)
        {
            public string Name;

            public string Type;

            public string TypeModuleName = paramType.Module.Name;

            public ParameterAttributes Attributes;

            public bool IsByRef;
        }
    }
}
