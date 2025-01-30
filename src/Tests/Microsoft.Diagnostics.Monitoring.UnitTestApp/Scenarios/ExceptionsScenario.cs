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
        public static Command Command()
        {
            Command singleExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.SingleException);
            singleExceptionCommand.SetAction(SingleExceptionAsync);

            Command multipleExceptionsCommand = new(TestAppScenarios.Exceptions.SubScenarios.MultipleExceptions);
            multipleExceptionsCommand.SetAction(MultipleExceptionsAsync);

            Command repeatExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.RepeatException);
            repeatExceptionCommand.SetAction(RepeatExceptionAsync);

            Command asyncExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.AsyncException);
            asyncExceptionCommand.SetAction(AsyncExceptionAsync);

            Command frameworkExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.FrameworkException);
            frameworkExceptionCommand.SetAction(FrameworkExceptionAsync);

            Command customExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.CustomException);
            customExceptionCommand.SetAction(CustomExceptionAsync);

            Command esotericStackFrameTypesCommand = new(TestAppScenarios.Exceptions.SubScenarios.EsotericStackFrameTypes);
            esotericStackFrameTypesCommand.SetAction(EsotericStackFrameTypesAsync);

            Command reversePInvokeExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.ReversePInvokeException);
            reversePInvokeExceptionCommand.SetAction(ReversePInvokeExceptionAsync);

            Command dynamicMethodExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.DynamicMethodException);
            dynamicMethodExceptionCommand.SetAction(DynamicMethodExceptionAsync);

            Command arrayExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.ArrayException);
            arrayExceptionCommand.SetAction(ArrayExceptionAsync);

            Command innerUnthrownExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.InnerUnthrownException);
            innerUnthrownExceptionCommand.SetAction(InnerUnthrownExceptionAsync);

            Command innerThrownExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.InnerThrownException);
            innerThrownExceptionCommand.SetAction(InnerThrownExceptionAsync);

            Command eclipsingExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.EclipsingException);
            eclipsingExceptionCommand.SetAction(EclipsingExceptionAsync);

            Command eclipsingExceptionFromMethodCallCommand = new(TestAppScenarios.Exceptions.SubScenarios.EclipsingExceptionFromMethodCall);
            eclipsingExceptionFromMethodCallCommand.SetAction(EclipsingExceptionFromMethodCallAsync);

            Command aggregateExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.AggregateException);
            aggregateExceptionCommand.SetAction(AggregateExceptionAsync);

            Command reflectionTypeLoadExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.ReflectionTypeLoadException);
            reflectionTypeLoadExceptionCommand.SetAction(ReflectionTypeLoadExceptionAsync);

            Command hiddenFramesExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.HiddenFramesExceptionCommand);
            hiddenFramesExceptionCommand.SetAction(HiddenFramesExceptionAsync);

            Command scenarioCommand = new(TestAppScenarios.Exceptions.Name);
            scenarioCommand.Subcommands.Add(singleExceptionCommand);
            scenarioCommand.Subcommands.Add(multipleExceptionsCommand);
            scenarioCommand.Subcommands.Add(repeatExceptionCommand);
            scenarioCommand.Subcommands.Add(asyncExceptionCommand);
            scenarioCommand.Subcommands.Add(frameworkExceptionCommand);
            scenarioCommand.Subcommands.Add(customExceptionCommand);
            scenarioCommand.Subcommands.Add(esotericStackFrameTypesCommand);
            scenarioCommand.Subcommands.Add(reversePInvokeExceptionCommand);
            scenarioCommand.Subcommands.Add(dynamicMethodExceptionCommand);
            scenarioCommand.Subcommands.Add(arrayExceptionCommand);
            scenarioCommand.Subcommands.Add(innerUnthrownExceptionCommand);
            scenarioCommand.Subcommands.Add(innerThrownExceptionCommand);
            scenarioCommand.Subcommands.Add(eclipsingExceptionCommand);
            scenarioCommand.Subcommands.Add(eclipsingExceptionFromMethodCallCommand);
            scenarioCommand.Subcommands.Add(aggregateExceptionCommand);
            scenarioCommand.Subcommands.Add(reflectionTypeLoadExceptionCommand);
            scenarioCommand.Subcommands.Add(hiddenFramesExceptionCommand);
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

        public static Task<int> MultipleExceptionsAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.Begin, logger);

                ThrowAndCatchInvalidOperationException();
                ThrowAndCatchCustomException();
                ThrowAndCatchArgumentNullExceptionFromFramework();

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

        public static Task<int> EsotericStackFrameTypesAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.Begin, logger);

                ThrowAndCatchEsotericStackFrameTypes();

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

        public static Task<int> InnerUnthrownExceptionAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.Begin, logger);

                ThrowAndCatchInvalidOperationException(includeInnerException: true, throwInnerException: false);

                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.End, logger);

                return 0;
            }, token);
        }

        public static Task<int> InnerThrownExceptionAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.Begin, logger);

                ThrowAndCatchInvalidOperationException(includeInnerException: true);

                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.End, logger);

                return 0;
            }, token);
        }

        public static Task<int> EclipsingExceptionFromMethodCallAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.Begin, logger);
                ThrowAndCatchEclipsingInvalidOperationExceptionFromMethodCall();
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.End, logger);
                return 0;
            }, token);
        }

        public static Task<int> EclipsingExceptionAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.Begin, logger);
                ThrowAndCatchEclipsingInvalidOperationException();
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.End, logger);
                return 0;
            }, token);
        }

        public static Task<int> AggregateExceptionAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.Begin, logger);

                try
                {
                    throw new AggregateException(
                        new InvalidOperationException(),
                        new FormatException(),
                        new TaskCanceledException());
                }
                catch (Exception)
                {
                }

                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.End, logger);

                return 0;
            }, token);
        }

        public static Task<int> ReflectionTypeLoadExceptionAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.Begin, logger);

                try
                {
                    throw new ReflectionTypeLoadException(
                        classes: null,
                        exceptions: new Exception[]
                        {
                            new MissingMethodException(),
                            null,
                            new MissingFieldException()
                        });
                }
                catch (Exception)
                {
                }

                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.End, logger);

                return 0;
            }, token);
        }

        public static Task<int> HiddenFramesExceptionAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.Begin, logger);
                try
                {
                    ThrowExceptionWithHiddenFrames();
                }
                catch (Exception)
                {
                }
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Exceptions.Commands.End, logger);
                return 0;
            }, token);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowExceptionWithHiddenFrames()
        {
            HiddenFrameTestMethods.EntryPoint(ThrowAndCatchInvalidOperationException);
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowAndCatchInvalidOperationException()
        {
            ThrowAndCatchInvalidOperationException(includeInnerException: false, throwInnerException: false);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowAndCatchInvalidOperationException(bool includeInnerException = false, bool throwInnerException = true)
        {
            try
            {
                Exception innerException = null;
                if (includeInnerException)
                {
                    if (throwInnerException)
                    {
                        try
                        {
                            throw new FormatException();
                        }
                        catch (Exception ex)
                        {
                            innerException = ex;
                        }
                    }
                    else
                    {
                        innerException = new FormatException();
                    }
                }

                throw new InvalidOperationException(null, innerException);
            }
            catch (Exception)
            {
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowAndCatchEclipsingInvalidOperationException()
        {
            try
            {
                try
                {
                    throw new FormatException();
                }
                catch (Exception innerException)
                {
                    throw new InvalidOperationException(null, innerException);
                }
            }
            catch (Exception)
            {
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowAndCatchEclipsingInvalidOperationExceptionFromMethodCall()
        {
            try
            {
                try
                {
                    throw new FormatException();
                }
                catch (Exception innerException)
                {
                    ThrowInvalidOperationExceptionWithInnerException(innerException);
                }
            }
            catch (Exception)
            {
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidOperationExceptionWithInnerException(Exception innerException)
        {
            throw new InvalidOperationException(null, innerException);
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe void ThrowAndCatchEsotericStackFrameTypes()
        {
            int i = 1;
            void* p = (void*)i;
            ThrowAndCatchEsotericStackFrameTypes(ref i, p);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe void ThrowAndCatchEsotericStackFrameTypes(ref int i, void* p)
        {
            try
            {
                throw new FormatException();
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
