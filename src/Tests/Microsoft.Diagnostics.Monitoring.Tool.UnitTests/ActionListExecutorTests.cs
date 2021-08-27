// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
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
        public async Task ActionListExecutor_AllActionsSucceed()
        {
            IHost host = SetUpHost();

            CollectionRuleOptions ruleOptions = new();
            ruleOptions.AddExecuteActionAppAction(new string[] { "ZeroExitCode" });
            ruleOptions.AddExecuteActionAppAction(new string[] { "ZeroExitCode" });

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

            await _executor.ExecuteActions(ruleOptions.Actions, null, cancellationTokenSource.Token);

            host.Dispose();
        }

        [Fact]
        public async Task ActionListExecutor_SecondActionFail()
        {
            IHost host = SetUpHost();

            CollectionRuleOptions ruleOptions = new();
            ruleOptions.AddExecuteActionAppAction(new string[] { "ZeroExitCode" });
            ruleOptions.AddExecuteActionAppAction(new string[] { "NonzeroExitCode" });

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

            CollectionRuleActionExecutionException actionExecutionException = await Assert.ThrowsAsync<CollectionRuleActionExecutionException>(
                () => _executor.ExecuteActions(ruleOptions.Actions, null, cancellationTokenSource.Token));

            Assert.Equal(1, actionExecutionException.ActionIndex);

            Assert.Contains(string.Format(Strings.ErrorMessage_NonzeroExitCode, "1"), actionExecutionException.Message);
            
            host.Dispose();
        }

        [Fact]
        public async Task ActionListExecutor_FirstActionFail()
        {
            IHost host = SetUpHost();

            CollectionRuleOptions ruleOptions = new();
            ruleOptions.AddExecuteActionAppAction(new string[] { "NonzeroExitCode" });
            ruleOptions.AddExecuteActionAppAction(new string[] { "ZeroExitCode" });

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

            CollectionRuleActionExecutionException actionExecutionException = await Assert.ThrowsAsync<CollectionRuleActionExecutionException>(
                () => _executor.ExecuteActions(ruleOptions.Actions, null, cancellationTokenSource.Token));

            Assert.Equal(0, actionExecutionException.ActionIndex);

            Assert.Contains(string.Format(Strings.ErrorMessage_NonzeroExitCode, "1"), actionExecutionException.Message);

            host.Dispose();
        }

        private IHost SetUpHost()
        {
            IHost host = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.ConfigureCollectionRules();
                    services.ConfigureEgress();
                })
                .Build();

            _executor = host.Services.GetService<ActionListExecutor>();

            return host;
        }
    }
}