﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using SampleMethods;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using static SampleMethods.StaticTestMethodSignatures;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.FunctionProbes
{
    internal static class FunctionProbesScenario
    {
        private delegate Task TestCaseAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, CancellationToken token);

        public static CliCommand Command()
        {
            Dictionary<string, TestCaseAsync> testCases = new()
            {
                /* Probe management */
                { TestAppScenarios.FunctionProbes.SubScenarios.ProbeInstallation, Test_ProbeInstallationAsync},
                { TestAppScenarios.FunctionProbes.SubScenarios.ProbeUninstallation, Test_ProbeUninstallationAsync},
                { TestAppScenarios.FunctionProbes.SubScenarios.ProbeReinstallation, Test_ProbeReinstallationAsync},

                /* Parameter capturing */
                { TestAppScenarios.FunctionProbes.SubScenarios.CapturePrimitives, Test_CapturePrimitivesAsync},
                { TestAppScenarios.FunctionProbes.SubScenarios.CaptureValueTypes, Test_CaptureValueTypesAsync},
                { TestAppScenarios.FunctionProbes.SubScenarios.CaptureImplicitThis, Test_CaptureImplicitThisAsync},
                { TestAppScenarios.FunctionProbes.SubScenarios.CaptureExplicitThis, Test_CaptureExplicitThisAsync},
                { TestAppScenarios.FunctionProbes.SubScenarios.CaptureNoParameters, Test_CaptureNoParametersAsync},
                { TestAppScenarios.FunctionProbes.SubScenarios.CaptureUnsupportedParameters, Test_CaptureUnsupportedParametersAsync},
                { TestAppScenarios.FunctionProbes.SubScenarios.CaptureValueTypeImplicitThis, Test_CaptureValueTypeImplicitThisAsync},

                /* Interesting functions */
                { TestAppScenarios.FunctionProbes.SubScenarios.AsyncMethod, Test_AsyncMethodAsync},
                { TestAppScenarios.FunctionProbes.SubScenarios.GenericMethods, Test_GenericMethodsAsync},
                { TestAppScenarios.FunctionProbes.SubScenarios.ExceptionRegionAtBeginningOfMethod, Test_ExceptionRegionAtBeginningOfMethodAsync},

                /* Fault injection */
                { TestAppScenarios.FunctionProbes.SubScenarios.ExceptionThrownByProbe, Test_ExceptionThrownByProbeAsync},
                { TestAppScenarios.FunctionProbes.SubScenarios.RecursingProbe, Test_RecursingProbeAsync},
                { TestAppScenarios.FunctionProbes.SubScenarios.RequestInstallationOnProbeFunction, Test_RequestInstallationOnProbeFunctionAsync},

                /* Self tests */
                { TestAppScenarios.FunctionProbes.SubScenarios.AssertsInProbesAreCaught, Test_AssertsInProbesAreCaughtAsync},

            };

            CliCommand scenarioCommand = new(TestAppScenarios.FunctionProbes.Name);
            foreach ((string subCommand, TestCaseAsync testCase) in testCases)
            {
                CliCommand testCaseCommand = new(subCommand);
                testCaseCommand.SetAction((result, token) =>
                {
                    return ScenarioHelpers.RunScenarioAsync(async logger =>
                    {
                        PerFunctionProbeProxy probeProxy = new PerFunctionProbeProxy();
                        using FunctionProbesManager probeManager = new(probeProxy);

                        await testCase(probeManager, probeProxy, token);

                        return 0;
                    }, token);
                });

                scenarioCommand.Subcommands.Add(testCaseCommand);
            }

            return scenarioCommand;
        }

        private static async Task Test_ProbeInstallationAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, CancellationToken token)
        {
            await WaitForProbeInstallationAsync(probeManager, probeProxy, Array.Empty<MethodInfo>(), token);
        }

        private static async Task Test_ProbeUninstallationAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, CancellationToken token)
        {
            await WaitForProbeInstallationAsync(probeManager, probeProxy, Array.Empty<MethodInfo>(), token);
            await WaitForProbeUninstallationAsync(probeManager, probeProxy, token);
        }

        private static async Task Test_ProbeReinstallationAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, CancellationToken token)
        {
            await WaitForProbeInstallationAsync(probeManager, probeProxy, Array.Empty<MethodInfo>(), token);
            await WaitForProbeUninstallationAsync(probeManager, probeProxy, token);
            await WaitForProbeInstallationAsync(probeManager, probeProxy, Array.Empty<MethodInfo>(), token);
        }

        private static async Task Test_CapturePrimitivesAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, CancellationToken token)
        {
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.Primitives));
            await RunStaticMethodTestCaseAsync(probeManager, probeProxy, method, new object[]
            {
                false,
                'c',
                sbyte.MinValue,
                byte.MaxValue,
                short.MinValue,
                ushort.MaxValue,
                int.MinValue,
                uint.MaxValue,
                long.MinValue,
                ulong.MaxValue,
                float.MaxValue,
                double.MaxValue
            }, token);
        }

        private static async Task Test_CaptureValueTypesAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, CancellationToken token)
        {
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.ValueType_TypeDef));
            await RunStaticMethodTestCaseAsync(probeManager, probeProxy, method, new object[]
            {
                MyEnum.ValueA
            }, token);
        }

        private static async Task Test_CaptureImplicitThisAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, CancellationToken token)
        {
            MethodInfo method = typeof(TestMethodSignatures).GetMethod(nameof(TestMethodSignatures.ImplicitThis));
            TestMethodSignatures testMethodSignatures = new();
            await RunInstanceMethodTestCaseAsync(probeManager, probeProxy, method, Array.Empty<object>(), testMethodSignatures, thisParameterSupported: true, token);
        }

        private static async Task Test_CaptureExplicitThisAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, CancellationToken token)
        {
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.ExplicitThis));
            TestMethodSignatures testMethodSignatures = new();
            await RunStaticMethodTestCaseAsync(probeManager, probeProxy, method, new object[]
            {
                testMethodSignatures
            }, token);
        }

        private static async Task Test_CaptureNoParametersAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, CancellationToken token)
        {
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.NoArgs));
            await RunStaticMethodTestCaseAsync(probeManager, probeProxy, method, Array.Empty<object>(), token);
        }

        private static async Task Test_ExceptionRegionAtBeginningOfMethodAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, CancellationToken token)
        {
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.ExceptionRegionAtBeginningOfMethod));
            await RunStaticMethodTestCaseAsync(probeManager, probeProxy, method, new object[]
            {
                null
            }, token);
        }

        private static async Task Test_AsyncMethodAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, CancellationToken token)
        {
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.AsyncMethod));
            await RunStaticMethodTestCaseAsync(probeManager, probeProxy, method, new object[]
            {
                5
            }, token);
        }

        private static async Task Test_GenericMethodsAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, CancellationToken token)
        {
            MethodInfo method = Type.GetType($"{nameof(SampleMethods)}.GenericTestMethodSignatures`2").GetMethod("GenericParameters");
            Assert.NotNull(method);

            probeProxy.RegisterPerFunctionProbe(method, (object[] actualArgs) => { });

            await WaitForProbeInstallationAsync(probeManager, probeProxy, new[] { method }, token);

            new GenericTestMethodSignatures<bool, int>().GenericParameters(false, 10, "hello world");
            new GenericTestMethodSignatures<string, object>().GenericParameters("", new object(), 10);
            new GenericTestMethodSignatures<MyEnum, Uri>().GenericParameters(MyEnum.ValueA, new Uri("https://www.bing.com"), new object());

            Assert.Equal(3, probeProxy.GetProbeInvokeCount(method));
        }

        public static async Task Test_CaptureValueTypeImplicitThisAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, CancellationToken token)
        {
            MethodInfo method = typeof(SampleNestedStruct).GetMethod(nameof(SampleNestedStruct.DoWork));
            SampleNestedStruct nestedStruct = new();
            await RunInstanceMethodTestCaseAsync(probeManager, probeProxy, method, new object[]
            {
                5
            }, nestedStruct, thisParameterSupported: false, token);
        }

        private static async Task Test_CaptureUnsupportedParametersAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, CancellationToken token)
        {
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.RefParam));

            // Use a custom invoker for the test since ref params can't be boxed.
            await RunTestCaseWithCustomInvokerAsync(probeManager, probeProxy, method, () =>
            {
                int i = 10;
                StaticTestMethodSignatures.RefParam(ref i);

                return Task.CompletedTask;
            },
            new object[]
            {
                null
            },
            thisObj: null, thisParameterSupported: false, token);
        }

        private static async Task Test_ExceptionThrownByProbeAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, CancellationToken token)
        {
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.ExceptionRegionAtBeginningOfMethod));
            probeProxy.RegisterPerFunctionProbe(method, (object[] actualArgs) =>
            {
                throw new InvalidOperationException();
            });

            await WaitForProbeInstallationAsync(probeManager, probeProxy, new[] { method }, token);

            StaticTestMethodSignatures.ExceptionRegionAtBeginningOfMethod(null);

            Assert.Equal(1, probeProxy.GetProbeInvokeCount(method));

            // Probes should be uninstalled now.
            await WaitForProbeUninstallationAsync(probeManager, probeProxy, token, explicitStopCaptureCall: false);
        }

        private static async Task Test_RecursingProbeAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, CancellationToken token)
        {
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.NoArgs));
            probeProxy.RegisterPerFunctionProbe(method, (object[] actualArgs) =>
            {
                StaticTestMethodSignatures.NoArgs();
            });

            await WaitForProbeInstallationAsync(probeManager, probeProxy, new[] { method }, token);

            StaticTestMethodSignatures.NoArgs();

            Assert.Equal(1, probeProxy.GetProbeInvokeCount(method));
        }

        private static async Task Test_RequestInstallationOnProbeFunctionAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, CancellationToken token)
        {
            MethodInfo method = typeof(FunctionProbesStub).GetMethod(nameof(FunctionProbesStub.EnterProbeStub));
            probeProxy.RegisterPerFunctionProbe(method, (object[] actualArgs) =>
            {
            });

            using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(5));

            // There's currently no notification mechanism for determining probe installation success, wait for timeout instead.
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await WaitForProbeInstallationAsync(probeManager, probeProxy, new[] { method }, timeoutSource.Token));
        }

        private static async Task Test_AssertsInProbesAreCaughtAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, CancellationToken token)
        {
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.NoArgs));

            await Assert.ThrowsAnyAsync<XunitException>(async () =>
            {
                // To force an assert in the test probes, call a method with no parameters but assert that a parameter is still captured.
                await RunTestCaseWithCustomInvokerAsync(probeManager, probeProxy, method, () =>
                {
                    StaticTestMethodSignatures.NoArgs();
                    return Task.CompletedTask;
                },
                new object[]
                {
                    5
                },
                thisObj: null, thisParameterSupported: false, token);
            });
        }

        private static Task RunInstanceMethodTestCaseAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, MethodInfo method, object[] args, object thisObj, bool thisParameterSupported, CancellationToken token)
        {
            Assert.False(method.IsStatic);
            return RunTestCaseWithStandardInvokerAsync(probeManager, probeProxy, method, args, thisObj, thisParameterSupported, token);
        }

        private static Task RunStaticMethodTestCaseAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, MethodInfo method, object[] args, CancellationToken token)
        {
            Assert.True(method.IsStatic);
            return RunTestCaseWithStandardInvokerAsync(probeManager, probeProxy, method, args, null, false, token);
        }

        private static Task RunTestCaseWithStandardInvokerAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, MethodInfo method, object[] args, object thisObj, bool thisParameterSupported, CancellationToken token)
        {
            return RunTestCaseWithCustomInvokerAsync(probeManager, probeProxy, method, async () =>
            {
                if (method.ReturnType.IsAssignableTo(typeof(Task)))
                {
                    await (Task)method.Invoke(thisObj, args);
                }
                else
                {
                    method.Invoke(thisObj, args);
                }
            }, args, thisObj, thisParameterSupported, token);
        }

        private static async Task RunTestCaseWithCustomInvokerAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, MethodInfo method, Func<Task> invoker, object[] args, object thisObj, bool thisParameterSupported, CancellationToken token)
        {
            Assert.NotNull(method);

            probeProxy.RegisterPerFunctionProbe(method, (object[] actualArgs) =>
            {
                if (thisObj != null)
                {
                    Assert.NotEmpty(actualArgs);
                    Assert.Equal(thisParameterSupported ? thisObj : null, actualArgs[0]);
                    Assert.Equal(args, actualArgs.Skip(1));
                }
                else
                {
                    Assert.Equal(args, actualArgs);
                }
            });

            await WaitForProbeInstallationAsync(probeManager, probeProxy, new[] { method }, token);

            await invoker().WaitAsync(token);

            Assert.Equal(1, probeProxy.GetProbeInvokeCount(method));

            if (probeProxy.TryGetProbeAssertException(method, out XunitException assertFailure))
            {
                throw assertFailure;
            }
        }

        private static async Task WaitForProbeUninstallationAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, CancellationToken token, bool explicitStopCaptureCall = true)
        {
            if (explicitStopCaptureCall)
            {
                probeManager.StopCapturing();
            }

            MethodInfo uninstallationTestMethod = typeof(FunctionProbesScenario).GetMethod(nameof(FunctionProbesScenario.UninstallationTestStub));
            Assert.NotNull(uninstallationTestMethod);

            probeProxy.RegisterPerFunctionProbe(uninstallationTestMethod, (object[] args) => { });

            while (!token.IsCancellationRequested)
            {
                int currentCount = probeProxy.GetProbeInvokeCount(uninstallationTestMethod);
                uninstallationTestMethod.Invoke(null, null);
                if (currentCount == probeProxy.GetProbeInvokeCount(uninstallationTestMethod))
                {
                    return;
                }

                await Task.Delay(100, token);
            }

            token.ThrowIfCancellationRequested();
        }

        private static async Task WaitForProbeInstallationAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, IList<MethodInfo> methods, CancellationToken token)
        {
            // Register the uninstallation test method as well so WaitForProbeUninstallationAsync can function
            MethodInfo uninstallationTestMethod = typeof(FunctionProbesScenario).GetMethod(nameof(FunctionProbesScenario.UninstallationTestStub));
            Assert.NotNull(uninstallationTestMethod);

            MethodInfo installationTestMethod = typeof(FunctionProbesScenario).GetMethod(nameof(FunctionProbesScenario.InstallationTestStub));
            Assert.NotNull(installationTestMethod);

            probeProxy.RegisterPerFunctionProbe(installationTestMethod, (object[] args) => { });

            List<MethodInfo> methodsToCapture = new(methods.Count + 2)
            {
                installationTestMethod,
                uninstallationTestMethod
            };
            methodsToCapture.AddRange(methods);
            probeManager.StartCapturing(methodsToCapture);

            while (!token.IsCancellationRequested)
            {
                installationTestMethod.Invoke(null, null);
                if (probeProxy.GetProbeInvokeCount(installationTestMethod) != 0)
                {
                    return;
                }

                await Task.Delay(100, token);
            }

            token.ThrowIfCancellationRequested();
        }

        public static void InstallationTestStub()
        {
        }
        public static void UninstallationTestStub()
        {
        }
    }
}
