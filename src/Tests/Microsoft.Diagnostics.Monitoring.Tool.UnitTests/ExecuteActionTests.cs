// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using System.Reflection;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using System.Threading;
using System;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class ExecuteActionTests
    {
        private const int TokenTimeoutMs = 2000; // Arbitrarily set

        // This should be identical to the error message found in Strings.resx (except without the process's exit code)
        private const string NonzeroExitCodeMessage = "The process exited with exit code";

        [Fact]
        public async Task ExecuteAction_ZeroExitCode()
        {
            ExecuteAction action = new();

            ExecuteOptions options = new();

            options.Path = DotNetHost.HostExePath;
            options.Arguments = Assembly.GetExecutingAssembly().Location.Replace(
                Assembly.GetExecutingAssembly().GetName().Name,
                "Microsoft.Diagnostics.Monitoring.ExecuteActionApp") + " ZeroExitCode";

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            CollectionRuleActionResult result = await action.ExecuteAsync(options, null, cancellationToken);

            Assert.Equal("0", result.OutputValues["ExitCode"]);
        }

        [Fact]
        public async Task ExecuteAction_NonzeroExitCode()
        {
            ExecuteAction action = new();

            ExecuteOptions options = new();

            options.Path = DotNetHost.HostExePath;
            options.Arguments = Assembly.GetExecutingAssembly().Location.Replace(
                Assembly.GetExecutingAssembly().GetName().Name,
                "Microsoft.Diagnostics.Monitoring.ExecuteActionApp") + " NonzeroExitCode";

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            InvalidOperationException invalidOperationException = await Assert.ThrowsAsync<InvalidOperationException>(
    () => action.ExecuteAsync(options, null, cancellationToken));
            Assert.Contains(NonzeroExitCodeMessage, invalidOperationException.Message);
        }

        [Fact]
        public async Task ExecuteAction_TokenCancellation()
        {
            ExecuteAction action = new();

            ExecuteOptions options = new();

            options.Path = DotNetHost.HostExePath;
            options.Arguments = Assembly.GetExecutingAssembly().Location.Replace(
                Assembly.GetExecutingAssembly().GetName().Name,
                "Microsoft.Diagnostics.Monitoring.ExecuteActionApp") + " TokenCancellation";

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            OperationCanceledException invalidOperationException = await Assert.ThrowsAsync<OperationCanceledException>(
    () => action.ExecuteAsync(options, null, cancellationToken));
            Assert.Contains(NonzeroExitCodeMessage, invalidOperationException.Message);
        }
    }
}
