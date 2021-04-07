﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTests.Runners
{
    internal static class AppRunnerExtensions
    {
        private static readonly TimeSpan ExceptionTimeout = TimeSpan.FromSeconds(5);

        public static async Task ExecuteAsync(this AppRunner runner, Func<Task> func)
        {
            try
            {
                await runner.StartAsync(TestTimeouts.StartProcess);

                await runner.SendStartScenarioAsync(TestTimeouts.SendCommand);

                await func();

                await runner.SendEndScenarioAsync(TestTimeouts.SendCommand);

                // This gives the app time to send out any remaining stdout/stderr messages,
                // exit properly, and delete its diagnostic pipe.
                await runner.WaitForExitAsync(TestTimeouts.WaitForExit);
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
                await runner.DisposeAsync();
            }
        }

        public static async Task ExecuteAsync(this IEnumerable<AppRunner> runners, Func<Task> func)
        {
            try
            {
                foreach (AppRunner runner in runners)
                {
                    await runner.StartAsync(TestTimeouts.StartProcess);

                    await runner.SendStartScenarioAsync(TestTimeouts.SendCommand);
                }

                await func();

                // This gives apps time to send out any remaining stdout/stderr messages,
                // exit properly, and delete their diagnostic pipes.
                await Task.WhenAll(runners.Select(async runner =>
                    {
                        await runner.SendEndScenarioAsync(TestTimeouts.SendCommand);

                        await runner.WaitForExitAsync(TestTimeouts.WaitForExit);
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
                await runners.DisposeItemsAsync();
            }
        }

        public static async Task SendEndScenarioAsync(this AppRunner runner, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            await runner.EndScenarioAsync(cancellation.Token).ConfigureAwait(false);
        }

        public static async Task SendCommandAsync(this AppRunner runner, string command, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            await runner.SendCommandAsync(command, cancellation.Token).ConfigureAwait(false);
        }

        public static async Task SendStartScenarioAsync(this AppRunner runner, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            await runner.StartScenarioAsync(cancellation.Token).ConfigureAwait(false);
        }

        public static async Task StartAsync(this AppRunner runner, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            await runner.StartAsync(cancellation.Token).ConfigureAwait(false);
        }

        public static async Task<int> WaitForExitAsync(this AppRunner runner, TimeSpan timeout)
        {
            using CancellationTokenSource cancellation = new(timeout);
            return await runner.WaitForExitAsync(cancellation.Token).ConfigureAwait(false);
        }
    }
}
