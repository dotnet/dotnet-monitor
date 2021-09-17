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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Xunit.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class ActionListExecutorTests
    {
        private const int TokenTimeoutMs = 10000;

        private readonly ITestOutputHelper _outputHelper;

        private const string DefaultRuleName = "Default";

        public ActionListExecutorTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task ActionListExecutor_AllActionsSucceed()
        {
            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddExecuteActionAppAction(new string[] { "ZeroExitCode" })
                    .AddExecuteActionAppAction(new string[] { "ZeroExitCode" })
                    .SetStartupTrigger();
            }, async host =>
            {
                ActionListExecutor executor = host.Services.GetService<ActionListExecutor>();

                using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

                CollectionRuleOptions ruleOptions = host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>().Get(DefaultRuleName);
                ILogger<CollectionRuleService> logger = host.Services.GetRequiredService<ILogger<CollectionRuleService>>();

                CollectionRuleContext context = new(DefaultRuleName, ruleOptions, null, logger);

                await executor.ExecuteActions(context, cancellationTokenSource.Token);
            });
        }

        [Fact]
        public async Task ActionListExecutor_SecondActionFail()
        {
            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddExecuteActionAppAction(new string[] { "ZeroExitCode" })
                    .AddExecuteActionAppAction(new string[] { "NonzeroExitCode" })
                    .SetStartupTrigger();
            }, async host =>
            {
                ActionListExecutor executor = host.Services.GetService<ActionListExecutor>();

                using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

                CollectionRuleOptions ruleOptions = host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>().Get(DefaultRuleName);
                ILogger<CollectionRuleService> logger = host.Services.GetRequiredService<ILogger<CollectionRuleService>>();

                CollectionRuleContext context = new(DefaultRuleName, ruleOptions, null, logger);

                CollectionRuleActionExecutionException actionExecutionException = await Assert.ThrowsAsync<CollectionRuleActionExecutionException>(
                    () => executor.ExecuteActions(context, cancellationTokenSource.Token));

                Assert.Equal(1, actionExecutionException.ActionIndex);

                Assert.Equal(string.Format(Strings.ErrorMessage_NonzeroExitCode, "1"), actionExecutionException.Message);
            });
        }

        [Fact]
        public async Task ActionListExecutor_FirstActionFail()
        {
            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddExecuteActionAppAction(new string[] { "NonzeroExitCode" })
                    .AddExecuteActionAppAction(new string[] { "ZeroExitCode" })
                    .SetStartupTrigger();
            }, async host =>
            {
                ActionListExecutor executor = host.Services.GetService<ActionListExecutor>();

                using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

                CollectionRuleOptions ruleOptions = host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>().Get(DefaultRuleName);
                ILogger<CollectionRuleService> logger = host.Services.GetRequiredService<ILogger<CollectionRuleService>>();

                CollectionRuleContext context = new(DefaultRuleName, ruleOptions, null, logger);

                CollectionRuleActionExecutionException actionExecutionException = await Assert.ThrowsAsync<CollectionRuleActionExecutionException>(
                    () => executor.ExecuteActions(context, cancellationTokenSource.Token));

                Assert.Equal(0, actionExecutionException.ActionIndex);

                Assert.Equal(string.Format(Strings.ErrorMessage_NonzeroExitCode, "1"), actionExecutionException.Message);
            });
        }
    }
}