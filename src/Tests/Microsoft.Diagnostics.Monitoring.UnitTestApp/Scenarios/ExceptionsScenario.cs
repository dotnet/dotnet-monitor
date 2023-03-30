// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.CommandLine;
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

            Command repeatExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.RepeatException);
            repeatExceptionCommand.SetAction(RepeatExceptionAsync);

            Command asyncExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.AsyncException);
            asyncExceptionCommand.SetAction(AsyncExceptionAsync);

            Command frameworkExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.FrameworkException);
            frameworkExceptionCommand.SetAction(FrameworkExceptionAsync);

            Command customExceptionCommand = new(TestAppScenarios.Exceptions.SubScenarios.CustomException);
            customExceptionCommand.SetAction(CustomExceptionAsync);

            Command scenarioCommand = new(TestAppScenarios.Exceptions.Name);
            scenarioCommand.Subcommands.Add(singleExceptionCommand);
            scenarioCommand.Subcommands.Add(repeatExceptionCommand);
            scenarioCommand.Subcommands.Add(asyncExceptionCommand);
            scenarioCommand.Subcommands.Add(frameworkExceptionCommand);
            scenarioCommand.Subcommands.Add(customExceptionCommand);
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
                throw new CustomException<int, string>("This is a custom exception message.");
            }
            catch (Exception)
            {
            }
        }

        private sealed class CustomException<T1, T2> : Exception
        {
            public CustomException(string message) : base(message) { }
        }
    }
}
