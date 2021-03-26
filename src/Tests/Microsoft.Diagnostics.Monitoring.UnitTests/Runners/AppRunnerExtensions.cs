// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTests.Runners
{
    internal static class AppRunnerExtensions
    {
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
    }
}
