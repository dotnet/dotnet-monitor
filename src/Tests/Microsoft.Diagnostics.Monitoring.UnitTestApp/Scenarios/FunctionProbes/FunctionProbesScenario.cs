// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using SampleMethods;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ObjectiveC;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static SampleMethods.StaticTestMethodSignatures;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.FunctionProbes
{
    internal static class FunctionProbesScenario
    {
        public static CliCommand Command()
        {
            CliCommand command = new(TestAppScenarios.FunctionProbes.Name);
            command.SetAction(ExecuteAsync);
            return command;
        }

        private delegate Task TestCase(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy);

        public static Task<int> ExecuteAsync(ParseResult result, CancellationToken token)
        {
            Dictionary<string, TestCase> testCases = new()
            {
                /* Probe management */
                { TestAppScenarios.FunctionProbes.Commands.ProbeInstallation, Test_ProbeInstallationAsync},
                { TestAppScenarios.FunctionProbes.Commands.ProbeUninstallation, Test_ProbeUninstallationAsync},
                { TestAppScenarios.FunctionProbes.Commands.ProbeReinstallation, Test_ProbeReinstallationAsync},

                /* Parameter capturing */
                { TestAppScenarios.FunctionProbes.Commands.CapturePrimitives, Test_CapturePrimitivesAsync},
                { TestAppScenarios.FunctionProbes.Commands.CaptureValueTypes, Test_CaptureValueTypesAsync},
                { TestAppScenarios.FunctionProbes.Commands.CaptureImplicitThis, Test_CaptureImplicitThisAsync},
                { TestAppScenarios.FunctionProbes.Commands.CaptureExplicitThis, Test_CaptureExplicitThisAsync},
                { TestAppScenarios.FunctionProbes.Commands.NoParameters, Test_NoParametersAsync},
                { TestAppScenarios.FunctionProbes.Commands.UnsupportedParameters, Test_UnsupportedParametersAsync},
                { TestAppScenarios.FunctionProbes.Commands.ValueTypeImplicitThis, Test_ValueTypeImplicitThisAsync},

                /* Interesting functions */
                { TestAppScenarios.FunctionProbes.Commands.GenericFunctions, Test_GenericFunctionsAsync},
                { TestAppScenarios.FunctionProbes.Commands.ExceptionRegionAtBeginningOfFunction, Test_ExceptionRegionAtBeginningOfFunctionAsync},

            };

            return ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                PerFunctionProbeProxy probeProxy = new PerFunctionProbeProxy();
                using FunctionProbesManager probeManager = new(probeProxy);

                string command = await ScenarioHelpers.WaitForCommandAsync(testCases.Keys.ToArray(), logger);
                await testCases[command](probeManager, probeProxy);
                probeProxy.ClearAllProbes();

                return 0;
            }, token);
        }

        private static async Task Test_ProbeInstallationAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy)
        {
            await WaitForProbeInstallationAsync(probeManager, probeProxy, Array.Empty<MethodInfo>(), CancellationToken.None);
        }

        private static async Task Test_ProbeUninstallationAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy)
        {
            await WaitForProbeInstallationAsync(probeManager, probeProxy, Array.Empty<MethodInfo>(), CancellationToken.None);
            await WaitForProbeUninstallationAsync(probeManager, probeProxy, CancellationToken.None);
        }

        private static async Task Test_ProbeReinstallationAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy)
        {
            await WaitForProbeInstallationAsync(probeManager, probeProxy, Array.Empty<MethodInfo>(), CancellationToken.None);
            await WaitForProbeUninstallationAsync(probeManager, probeProxy, CancellationToken.None);
            await WaitForProbeInstallationAsync(probeManager, probeProxy, Array.Empty<MethodInfo>(), CancellationToken.None);
        }

        private static async Task Test_CapturePrimitivesAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy)
        {
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.Primitives));
            await RunTestCaseAsync(probeManager, probeProxy, method, new object[]
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
            });
        }

        private static async Task Test_CaptureValueTypesAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy)
        {
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.ValueType_TypeDef));
            await RunTestCaseAsync(probeManager, probeProxy, method, new object[]
            {
                MyEnum.ValueA
            });
        }

        private static async Task Test_CaptureImplicitThisAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy)
        {
            MethodInfo method = typeof(TestMethodSignatures).GetMethod(nameof(TestMethodSignatures.ImplicitThis));
            TestMethodSignatures testMethodSignatures = new();
            await RunTestCaseAsync(probeManager, probeProxy, method, Array.Empty<object>(), testMethodSignatures);
        }

        private static async Task Test_CaptureExplicitThisAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy)
        {
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.ExplicitThis));
            TestMethodSignatures testMethodSignatures = new();
            await RunTestCaseAsync(probeManager, probeProxy, method, new object[]
            {
                testMethodSignatures
            });
        }

        private static async Task Test_NoParametersAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy)
        {
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.NoArgs));
            await RunTestCaseAsync(probeManager, probeProxy, method, Array.Empty<object>());
        }

        private static async Task Test_ExceptionRegionAtBeginningOfFunctionAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy)
        {
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.ExceptionRegionAtBeginningOfFunction));
            await RunTestCaseAsync(probeManager, probeProxy, method, new object[]
            {
                null
            });
        }

        private static async Task Test_GenericFunctionsAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy)
        {
            MethodInfo method = Type.GetType($"{nameof(SampleMethods)}.GenericTestMethodSignatures`2").GetMethod("GenericParameters");
            Assert.NotNull(method);

            probeProxy.RegisterPerFunctionProbe(method, (object[] actualArgs) => { });

            await WaitForProbeInstallationAsync(probeManager, probeProxy, new[] { method }, CancellationToken.None);

            new GenericTestMethodSignatures<bool, int>().GenericParameters(false, 10, "hello world");
            new GenericTestMethodSignatures<string, object>().GenericParameters("", new object(), 10);
            new GenericTestMethodSignatures<MyEnum, Uri>().GenericParameters(MyEnum.ValueA, new Uri("https://www.bing.com"), new object());

            Assert.Equal(3, probeProxy.GetProbeInvokeCount(method));
        }

        public static async Task Test_ValueTypeImplicitThisAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy)
        {
            MethodInfo method = typeof(SampleNestedStruct).GetMethod(nameof(SampleNestedStruct.DoWork));
            SampleNestedStruct nestedStruct = new();
            await RunTestCaseAsync(probeManager, probeProxy, method, new object[]
            {
                5
            }, nestedStruct, capturedThisObj: null);
        }

        private static async Task Test_UnsupportedParametersAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy)
        {
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.RefParam));
            Assert.NotNull(method);

            probeProxy.RegisterPerFunctionProbe(method, (object[] actualArgs) =>
            {
                object arg1 = Assert.Single(actualArgs);
                Assert.Null(arg1);
            });

            await WaitForProbeInstallationAsync(probeManager, probeProxy, new[] { method }, CancellationToken.None);

            int i = 10;
            StaticTestMethodSignatures.RefParam(ref i);

            Assert.Equal(1, probeProxy.GetProbeInvokeCount(method));
        }

        private static Task RunTestCaseAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, MethodInfo method, object[] args, object thisObj)
        {
            return RunTestCaseAsync(probeManager, probeProxy, method, args, thisObj, thisObj);
        }

        private static async Task RunTestCaseAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, MethodInfo method, object[] args, object thisObj = null, object capturedThisObj = null)
        {
            Assert.NotNull(method);

            probeProxy.RegisterPerFunctionProbe(method, (object[] actualArgs) =>
            {
                if (thisObj != null)
                {
                    Assert.NotEmpty(actualArgs);
                    Assert.Equal(capturedThisObj, actualArgs[0]);
                    Assert.Equal(args, actualArgs.Skip(1));
                }
                else
                {
                    Assert.Equal(args, actualArgs);
                }
            });

            await WaitForProbeInstallationAsync(probeManager, probeProxy, new[] { method }, CancellationToken.None);

            method.Invoke(thisObj, args);

            Assert.Equal(1, probeProxy.GetProbeInvokeCount(method));
        }

        private static async Task WaitForProbeUninstallationAsync(FunctionProbesManager probeManager, PerFunctionProbeProxy probeProxy, CancellationToken token)
        {
            probeManager.StopCapturing();

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

                await Task.Delay(100, token).ConfigureAwait(false);
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

                await Task.Delay(100, token).ConfigureAwait(false);
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
