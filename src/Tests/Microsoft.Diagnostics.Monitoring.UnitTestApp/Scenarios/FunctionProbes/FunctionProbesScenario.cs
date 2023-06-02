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
using System.Threading;
using System.Threading.Tasks;
using Xunit;

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

        private delegate Task TestCase(FunctionProbesManager probeManager, FunctionProbeRedirector probeRedirector);

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

                /* Interesting functions */
                { TestAppScenarios.FunctionProbes.Commands.GenericFunctions, Test_GenericFunctionsAsync},
                { TestAppScenarios.FunctionProbes.Commands.ExceptionRegionAtBeginningOfFunction, Test_ExceptionRegionAtBeginningOfFunctionAsync},

                /* Fault injection */
                { TestAppScenarios.FunctionProbes.Commands.ExceptionThrowingProbe, Test_FaultInjection_ExceptionThrowingProbeAsync}

            };

            return ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                FunctionProbeRedirector probeRedirector = new FunctionProbeRedirector();
                using FunctionProbesManager probeManager = new();
                FunctionProbesManager.SetFunctionProbes(probeRedirector);

                string command = await ScenarioHelpers.WaitForCommandAsync(testCases.Keys.ToArray(), logger);
                await testCases[command](probeManager, probeRedirector);

                return 0;
            }, token);
        }

        private static async Task Test_ProbeInstallationAsync(FunctionProbesManager probeManager, FunctionProbeRedirector probeRedirector)
        {
            await WaitForProbeInstallationAsync(probeManager, probeRedirector, Array.Empty<MethodInfo>(), CancellationToken.None);
        }

        private static async Task Test_ProbeUninstallationAsync(FunctionProbesManager probeManager, FunctionProbeRedirector probeRedirector)
        {
            await WaitForProbeInstallationAsync(probeManager, probeRedirector, Array.Empty<MethodInfo>(), CancellationToken.None);
            await WaitForProbeUninstallationAsync(probeManager, probeRedirector, CancellationToken.None);
        }

        private static async Task Test_ProbeReinstallationAsync(FunctionProbesManager probeManager, FunctionProbeRedirector probeRedirector)
        {
            await WaitForProbeInstallationAsync(probeManager, probeRedirector, Array.Empty<MethodInfo>(), CancellationToken.None);
            await WaitForProbeUninstallationAsync(probeManager, probeRedirector, CancellationToken.None);
            await WaitForProbeInstallationAsync(probeManager, probeRedirector, Array.Empty<MethodInfo>(), CancellationToken.None);
        }

        private static async Task Test_CapturePrimitivesAsync(FunctionProbesManager probeManager, FunctionProbeRedirector probeRedirector)
        {
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.Primitives));
            await RunTestCaseAsync(probeManager, probeRedirector, method, new object[]
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

        private static async Task Test_CaptureValueTypesAsync(FunctionProbesManager probeManager, FunctionProbeRedirector probeRedirector)
        {
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.ValueType_TypeDef));
            await RunTestCaseAsync(probeManager, probeRedirector, method, new object[]
            {
                MyEnum.ValueA
            });
        }

        private static async Task Test_CaptureImplicitThisAsync(FunctionProbesManager probeManager, FunctionProbeRedirector probeRedirector)
        {
            MethodInfo method = typeof(TestMethodSignatures).GetMethod(nameof(TestMethodSignatures.ImplicitThis));
            TestMethodSignatures testMethodSignatures = new();
            await RunTestCaseAsync(probeManager, probeRedirector, method, Array.Empty<object>(), testMethodSignatures);
        }

        private static async Task Test_CaptureExplicitThisAsync(FunctionProbesManager probeManager, FunctionProbeRedirector probeRedirector)
        {
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.ExplicitThis));
            TestMethodSignatures testMethodSignatures = new();
            await RunTestCaseAsync(probeManager, probeRedirector, method, new object[]
            {
                testMethodSignatures
            });
        }

        private static async Task Test_NoParametersAsync(FunctionProbesManager probeManager, FunctionProbeRedirector probeRedirector)
        {
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.NoArgs));
            await RunTestCaseAsync(probeManager, probeRedirector, method, Array.Empty<object>());
        }

        private static async Task Test_ExceptionRegionAtBeginningOfFunctionAsync(FunctionProbesManager probeManager, FunctionProbeRedirector probeRedirector)
        {
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.ExceptionRegionAtBeginningOfFunction));
            await RunTestCaseAsync(probeManager, probeRedirector, method, new object[]
            {
                null
            });
        }

        private static async Task Test_GenericFunctionsAsync(FunctionProbesManager probeManager, FunctionProbeRedirector probeRedirector)
        {
            MethodInfo method = Type.GetType($"{nameof(SampleMethods)}.GenericTestMethodSignatures`2").GetMethod("GenericParameters");
            Assert.NotNull(method);

            probeRedirector.RegisterPerFunctionProbe(method, (object[] actualArgs) => { });

            await WaitForProbeInstallationAsync(probeManager, probeRedirector, new[] { method }, CancellationToken.None);

            new GenericTestMethodSignatures<bool, int>().GenericParameters(false, 10, "hello world");
            new GenericTestMethodSignatures<string, object>().GenericParameters("", new object(), 10);
            new GenericTestMethodSignatures<MyEnum, Uri>().GenericParameters(MyEnum.ValueA, new Uri("https://www.bing.com"), new object());

            Assert.Equal(3, probeRedirector.GetProbeInvokeCount(method));
        }


        private static async Task Test_FaultInjection_ExceptionThrowingProbeAsync(FunctionProbesManager probeManager, FunctionProbeRedirector probeRedirector)
        {
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.NoArgs));
            Assert.NotNull(method);

            probeRedirector.RegisterPerFunctionProbe(method, (object[] actualArgs) =>
            {
                throw new Exception("Exception thrown in faulty probe");
            });

            await WaitForProbeInstallationAsync(probeManager, probeRedirector, new[] { method }, CancellationToken.None);

            // Should not throw.
            method.Invoke(null, null);

            Assert.Equal(1, probeRedirector.GetProbeInvokeCount(method));
        }

        private static async Task Test_UnsupportedParametersAsync(FunctionProbesManager probeManager, FunctionProbeRedirector probeRedirector)
        {
            MethodInfo method = typeof(StaticTestMethodSignatures).GetMethod(nameof(StaticTestMethodSignatures.RefParam));
            Assert.NotNull(method);

            probeRedirector.RegisterPerFunctionProbe(method, (object[] actualArgs) =>
            {
                object arg1 = Assert.Single(actualArgs);
                Assert.Null(arg1);
            });

            await WaitForProbeInstallationAsync(probeManager, probeRedirector, new[] { method }, CancellationToken.None);

            int i = 10;
            StaticTestMethodSignatures.RefParam(ref i);

            Assert.Equal(1, probeRedirector.GetProbeInvokeCount(method));
        }

        private static async Task RunTestCaseAsync(FunctionProbesManager probeManager, FunctionProbeRedirector probeRedirector, MethodInfo method, object[] args, object thisObj = null)
        {
            Assert.NotNull(method);

            probeRedirector.RegisterPerFunctionProbe(method, (object[] actualArgs) =>
            {
                if (thisObj != null)
                {
                    Assert.NotEmpty(actualArgs);
                    Assert.Equal(thisObj, actualArgs[0]);
                    Assert.Equal(args, actualArgs.Skip(1));
                }
                else
                {
                    Assert.Equal(args, actualArgs);
                }
            });

            await WaitForProbeInstallationAsync(probeManager, probeRedirector, new[] { method }, CancellationToken.None);

            method.Invoke(thisObj, args);

            Assert.Equal(1, probeRedirector.GetProbeInvokeCount(method));
        }

        private static async Task WaitForProbeUninstallationAsync(FunctionProbesManager probeManager, FunctionProbeRedirector probeRedirector, CancellationToken token)
        {
            // TODO: Lifetime on this is fuzzy
            ManualResetEventSlim probeHit = new(initialState: false);
            TaskCompletionSource<bool> probeUninstalled = new();

            MethodInfo methodStub = typeof(FunctionProbesScenario).GetMethod(nameof(FunctionProbesScenario.UninstallationTestStub));
            Assert.NotNull(methodStub);
            probeRedirector.RegisterPerFunctionProbe(methodStub, (object[] args) =>
            {
                probeHit.Set();
            });

            probeManager.StopCapturing();

            using CancellationTokenSource stopPokingStub = CancellationTokenSource.CreateLinkedTokenSource(token);
            CancellationToken pokerToken = stopPokingStub.Token;
            Task stubRunner = Task.Run(async () =>
            {
                while (!pokerToken.IsCancellationRequested)
                {
                    UninstallationTestStub();
                    if (probeHit.IsSet)
                    {
                        probeHit.Reset();
                    }
                    else
                    {
                        probeUninstalled.SetResult(true);
                    }

                    await Task.Delay(100).ConfigureAwait(false);
                }
            }, pokerToken);

            await probeUninstalled.Task.WaitAsync(token).ConfigureAwait(false);
        }

        private static async Task WaitForProbeInstallationAsync(FunctionProbesManager probeManager, FunctionProbeRedirector probeRedirector, IList<MethodInfo> methods, CancellationToken token)
        {
            TaskCompletionSource<bool> probeInstalled = new();

            MethodInfo methodStub = typeof(FunctionProbesScenario).GetMethod(nameof(FunctionProbesScenario.InstallationTestStub));
            Assert.NotNull(methodStub);
            probeRedirector.RegisterPerFunctionProbe(methodStub, (object[] args) =>
            {
                probeInstalled.SetResult(true);
            });

            List<MethodInfo> methodsToCapture = new(methods.Count + 1)
            {
                methodStub
            };
            methodsToCapture.AddRange(methods);
            probeManager.StartCapturing(methodsToCapture);

            using CancellationTokenSource stopPokingStub = CancellationTokenSource.CreateLinkedTokenSource(token);

            CancellationToken pokerToken = stopPokingStub.Token;
            Task stubRunner = Task.Run(async () =>
            {
                while (!pokerToken.IsCancellationRequested)
                {
                    InstallationTestStub();
                    await Task.Delay(100).ConfigureAwait(false);
                }
            }, pokerToken);

            await probeInstalled.Task.WaitAsync(token).ConfigureAwait(false);
        }

        public static void InstallationTestStub()
        {
        }
        public static void UninstallationTestStub()
        {
        }
    }
}
