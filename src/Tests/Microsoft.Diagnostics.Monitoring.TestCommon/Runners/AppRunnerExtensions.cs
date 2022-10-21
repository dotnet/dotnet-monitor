// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.TestCommon.Runners
{
    public static class AppRunnerExtensions
    {
        private static readonly TimeSpan ExceptionTimeout = TimeSpan.FromSeconds(5);

        public static string GetLocalhostUrl(this AppRunner runner)
        {
            UriBuilder builder = new(runner.BoundUrl);
            builder.Host = "localhost";
            return builder.ToString();
        }

        public static async Task ExecuteAsync(this AppRunner runner, Func<Task> func)
        {
            try
            {
                await runner.StartAsync();

                await runner.SendStartScenarioAsync();

                await func();

                await runner.SendEndScenarioAsync();

                // This gives the app time to send out any remaining stdout/stderr messages,
                // exit properly, and delete its diagnostic pipe.
                await runner.WaitForExitAsync();
            }
            catch (Exception)
            {
                // If an exception is thrown, give app some time to send out any remaining
                // stdout/stderr messages.
                await Task.Delay(ExceptionTimeout);

                throw;
            }
            finally
            {
                await runner.StopAsync();
            }
        }

        public static async Task ExecuteAsync(this IEnumerable<AppRunner> runners, Func<Task> func)
        {
            try
            {
                foreach (AppRunner runner in runners)
                {
                    await runner.StartAsync();

                    await runner.SendStartScenarioAsync();
                }

                await func();

                // This gives apps time to send out any remaining stdout/stderr messages,
                // exit properly, and delete their diagnostic pipes.
                await Task.WhenAll(runners.Select(async runner =>
                    {
                        await runner.SendEndScenarioAsync();

                        await runner.WaitForExitAsync();
                    }));
            }
            catch (Exception)
            {
                // If an exception is thrown, give app some time to send out any remaining
                // stdout/stderr messages.
                await Task.Delay(ExceptionTimeout);

                throw;
            }
            finally
            {
                foreach (AppRunner runner in runners)
                {
                    await runner.StopAsync();
                }
            }
        }

        public static Task SendEndScenarioAsync(this AppRunner runner)
        {
            return runner.SendEndScenarioAsync(CommonTestTimeouts.SendCommand);
        }

        public static async Task SendEndScenarioAsync(this AppRunner runner, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            await runner.EndScenarioAsync(cancellation.Token).ConfigureAwait(false);
        }

        public static Task SendCommandAsync(this AppRunner runner, string command)
        {
            return runner.SendCommandAsync(command, CommonTestTimeouts.SendCommand);
        }

        public static async Task SendCommandAsync(this AppRunner runner, string command, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            await runner.SendCommandAsync(command, cancellation.Token).ConfigureAwait(false);
        }

        public static Task SendStartScenarioAsync(this AppRunner runner)
        {
            return runner.SendStartScenarioAsync(CommonTestTimeouts.SendCommand);
        }

        public static async Task SendStartScenarioAsync(this AppRunner runner, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            await runner.StartScenarioAsync(cancellation.Token).ConfigureAwait(false);
        }

        public static Task StartAsync(this AppRunner runner)
        {
            return runner.StartAsync(CommonTestTimeouts.StartProcess);
        }

        public static async Task StartAsync(this AppRunner runner, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            await runner.StartAsync(cancellation.Token).ConfigureAwait(false);
        }

        public static Task StopAsync(this AppRunner runner)
        {
            return runner.StopAsync(CommonTestTimeouts.StopProcess);
        }

        public static async Task StopAsync(this AppRunner runner, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            await runner.StopAsync(cancellation.Token).ConfigureAwait(false);
        }

        public static async Task<string> GetEnvironmentVariable(this AppRunner runner, string name, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            ConfiguredTaskAwaitable<string> getVarTask = runner.WaitForEnvironmentVariable(name, cancellation.Token).ConfigureAwait(false);
            await runner.SendCommandAsync(TestAppScenarios.Commands.PrintEnvironmentVariables).ConfigureAwait(false);
            string result = await getVarTask;
            return result;
        }

        public static Task<int> WaitForExitAsync(this AppRunner runner)
        {
            return runner.WaitForExitAsync(CommonTestTimeouts.WaitForExit);
        }

        public static async Task<int> WaitForExitAsync(this AppRunner runner, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            return await runner.WaitForExitAsync(cancellation.Token).ConfigureAwait(false);
        }
    }
}
