// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using System.Threading;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class ActionListExecutorTests
    {
        private const int TokenTimeoutMs = 10000;

        private ActionListExecutor _executor;

        [Fact]
        public async Task ActionListExecutor_MultipleExecute_Zero_Zero()
        {
            SetUpHost();

            CollectionRuleOptions ruleOptions = new RootOptions()
                .CreateCollectionRule("Default")
                .AddExecuteAction(DotNetHost.HostExePath, ExecuteActionTests.GenerateArgumentsString(new string[] { "ZeroExitCode" }))
                .AddExecuteAction(DotNetHost.HostExePath, ExecuteActionTests.GenerateArgumentsString(new string[] { "ZeroExitCode" }));

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

            await _executor.ExecuteActions(ruleOptions.Actions, null, cancellationTokenSource.Token);
        }

        [Fact]
        public async Task ActionListExecutor_MultipleExecute_Zero_Nonzero()
        {
            SetUpHost();

            CollectionRuleOptions ruleOptions = new RootOptions()
                .CreateCollectionRule("Default")
                .AddExecuteAction(DotNetHost.HostExePath, ExecuteActionTests.GenerateArgumentsString(new string[] { "ZeroExitCode" }))
                .AddExecuteAction(DotNetHost.HostExePath, ExecuteActionTests.GenerateArgumentsString(new string[] { "NonzeroExitCode" }));

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

            CollectionRuleActionExecutionException actionExecutionException = await Assert.ThrowsAsync<CollectionRuleActionExecutionException>(
                () => _executor.ExecuteActions(ruleOptions.Actions, null, cancellationTokenSource.Token));

            Assert.Equal(1, actionExecutionException.ActionIndex);

            Assert.Contains(string.Format(Strings.ErrorMessage_NonzeroExitCode, "1"), actionExecutionException.Message);
        }

        [Fact]
        public async Task ActionListExecutor_MultipleExecute_NonZero_Zero()
        {
            SetUpHost();

            CollectionRuleOptions ruleOptions = new RootOptions()
                .CreateCollectionRule("Default")
                .AddExecuteAction(DotNetHost.HostExePath, ExecuteActionTests.GenerateArgumentsString(new string[] { "NonzeroExitCode" }))
                .AddExecuteAction(DotNetHost.HostExePath, ExecuteActionTests.GenerateArgumentsString(new string[] { "ZeroExitCode" }));

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

            CollectionRuleActionExecutionException actionExecutionException = await Assert.ThrowsAsync<CollectionRuleActionExecutionException>(
                () => _executor.ExecuteActions(ruleOptions.Actions, null, cancellationTokenSource.Token));

            Assert.Equal(0, actionExecutionException.ActionIndex);

            Assert.Contains(string.Format(Strings.ErrorMessage_NonzeroExitCode, "1"), actionExecutionException.Message);
        }

        internal void SetUpHost()
        {
            IHost host = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.ConfigureCollectionRules();
                    services.ConfigureEgress();
                })
                .Build();

            _executor = host.Services.GetService<ActionListExecutor>();
        }
    }
}