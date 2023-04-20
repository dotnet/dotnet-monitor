// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.CommandLine;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    internal static class ExceptionsScenario
    {
        public static CliCommand Command()
        {
            CliCommand singleExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.SingleException);
            singleExceptionCommand.SetAction(SingleExceptionAsync);

            CliCommand repeatExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.RepeatException);
            repeatExceptionCommand.SetAction(RepeatExceptionAsync);

            CliCommand asyncExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.AsyncException);
            asyncExceptionCommand.SetAction(AsyncExceptionAsync);

            CliCommand frameworkExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.FrameworkException);
            frameworkExceptionCommand.SetAction(FrameworkExceptionAsync);

            CliCommand customExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.CustomException);
            customExceptionCommand.SetAction(CustomExceptionAsync);

            CliCommand reversePInvokeExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.ReversePInvokeException);
            reversePInvokeExceptionCommand.SetAction(ReversePInvokeExceptionAsync);

            CliCommand dynamicMethodExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.DynamicMethodException);
            dynamicMethodExceptionCommand.SetAction(DynamicMethodExceptionAsync);

            CliCommand arrayExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.ArrayException);
            arrayExceptionCommand.SetAction(ArrayExceptionAsync);

            CliCommand scenarioCommand = new(TestAppScenarios.Exceptions.Name);
            scenarioCommand.Subcommands.Add(singleExceptionCommand);
            scenarioCommand.Subcommands.Add(repeatExceptionCommand);
            scenarioCommand.Subcommands.Add(asyncExceptionCommand);
            scenarioCommand.Subcommands.Add(frameworkExceptionCommand);
            scenarioCommand.Subcommands.Add(customExceptionCommand);
            scenarioCommand.Subcommands.Add(reversePInvokeExceptionCommand);
            scenarioCommand.Subcommands.Add(dynamicMethodExceptionCommand);
            scenarioCommand.Subcommands.Add(arrayExceptionCommand);
            return scenarioCommand;
        }

        public static Task<int> SingleExceptionAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.Begin, logger);

                ThrowAndCatchInvalidOperationException();

                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.End, logger);

                return 0;
            }, token);
        }

        public static Task<int> RepeatExceptionAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.Begin, logger);

                ThrowAndCatchInvalidOperationException();

                ThrowAndCatchInvalidOperationException();

                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.End, logger);

                return 0;
            }, token);
        }

        public static Task<int> AsyncExceptionAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.Begin, logger);

                await ThrowAndCatchTaskCancellationExceptionAsync();

                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.End, logger);

                return 0;
            }, token);
        }

        public static Task<int> FrameworkExceptionAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.Begin, logger);

                ThrowAndCatchArgumentNullExceptionFromFramework();

                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.End, logger);

                return 0;
            }, token);
        }

        public static Task<int> CustomExceptionAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.Begin, logger);

                ThrowAndCatchCustomException();

                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.End, logger);

                return 0;
            }, token);
        }

        public static Task<int> ReversePInvokeExceptionAsync(ParseResult result, CancellationToken token)
        {
            MonitorLibrary.InitializeResolver();

            return ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.Begin, logger);

                MonitorLibrary.TestHook(ThrowAndCatchInvalidOperationException);

                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.End, logger);

                return 0;
            }, token);
        }

        public static Task<int> DynamicMethodExceptionAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.Begin, logger);

                // The following dynamic method is effectively this code:

                // public static void ThrowAndCatchFromDynamicMethod()
                // {
                //     try
                //     {
                //         throw new CustomSimpleException("Thrown from dynamic method!");
                //     }
                //     catch (Exception)
                //     {
                //     }
                // }

                DynamicMethod dynamicMethod = new DynamicMethod(
                    "ThrowAndCatchFromDynamicMethod",
                    MethodAttributes.Public | MethodAttributes.Static,
                    CallingConventions.Standard,
                    typeof(void),
                    Array.Empty<Type>(),
                    typeof(ExceptionsScenario),
                    skipVisibility: false);

                ILGenerator generator = dynamicMethod.GetILGenerator();

                Label leaveLabel = generator.DefineLabel();
                generator.BeginExceptionBlock();
                generator.Emit(OpCodes.Ldstr, "Thrown from dynamic method!");
                generator.Emit(OpCodes.Newobj, typeof(CustomSimpleException).GetConstructor(new Type[] { typeof(string) }));
                generator.Emit(OpCodes.Throw);
                generator.BeginCatchBlock(typeof(Exception));
                generator.Emit(OpCodes.Pop);
                generator.Emit(OpCodes.Leave_S, leaveLabel);
                generator.EndExceptionBlock();
                generator.MarkLabel(leaveLabel);
                generator.Emit(OpCodes.Ret);

                Action dynamicMethodDelegate = (Action)dynamicMethod.CreateDelegate(typeof(Action));

                dynamicMethodDelegate();

                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.End, logger);

                return 0;
            }, token);
        }

        public static Task<int> ArrayExceptionAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.Begin, logger);

                int[] values = Enumerable.Range(1, 100).ToArray();
                try
                {
                    object value = values.GetValue(200);
                }
                catch (Exception)
                {
                }

                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.End, logger);

                return 0;
            }, token);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowAndCatchInvalidOperationException()
        {
            try
            {
                throw new InvalidOperationException();
            }
            catch (Exception)
            {
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task ThrowAndCatchTaskCancellationExceptionAsync()
        {
            using CancellationTokenSource source = new();
            CancellationToken token = source.Token;

            Task innerTask = Task.Run(
                () => Task.Delay(Timeout.InfiniteTimeSpan, token),
                token);

            try
            {
                source.Cancel();
                await innerTask;
            }
            catch (Exception)
            {
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowAndCatchArgumentNullExceptionFromFramework()
        {
            try
            {
                object value = null;
                ArgumentNullException.ThrowIfNull(value, "myParameter");
            }
            catch (Exception)
            {
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowAndCatchCustomException()
        {
            try
            {
                throw new CustomGenericsException<int, string>("This is a custom exception message.");
            }
            catch (Exception)
            {
            }
        }

        private sealed class CustomSimpleException : Exception
        {
            public CustomSimpleException(string message) : base(message) { }
        }

        private sealed class CustomGenericsException<T1, T2> : Exception
        {
            public CustomGenericsException(string message) : base(message) { }
        }
    }
}
