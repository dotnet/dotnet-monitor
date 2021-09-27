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
using System;

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

                bool calledStartCallback = false;
                Action startCallback = () => calledStartCallback = true;

                await executor.ExecuteActions(context, startCallback, cancellationTokenSource.Token);

                Assert.True(calledStartCallback, "Expected start callback to have been invoked.");
            });
        }

        [Fact]
        public Task ActionListExecutor_SecondActionFail_DeferredCompletion()
        {
            return ActionListExecutor_SecondActionFail(waitForCompletion: false);
        }

        [Fact]
        public Task ActionListExecutor_SecondActionFail_WaitedCompletion()
        {
            return ActionListExecutor_SecondActionFail(waitForCompletion: true);
        }

        private async Task ActionListExecutor_SecondActionFail(bool waitForCompletion)
        {
            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddExecuteActionAppAction(waitForCompletion, new string[] { "ZeroExitCode" })
                    .AddExecuteActionAppAction(waitForCompletion, new string[] { "NonzeroExitCode" })
                    .SetStartupTrigger();
            }, async host =>
            {
                ActionListExecutor executor = host.Services.GetService<ActionListExecutor>();

                using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

                CollectionRuleOptions ruleOptions = host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>().Get(DefaultRuleName);
                ILogger<CollectionRuleService> logger = host.Services.GetRequiredService<ILogger<CollectionRuleService>>();

                CollectionRuleContext context = new(DefaultRuleName, ruleOptions, null, logger);

                bool calledStartCallback = false;
                Action startCallback = () => calledStartCallback = true;

                CollectionRuleActionExecutionException actionExecutionException = await Assert.ThrowsAsync<CollectionRuleActionExecutionException>(
                    () => executor.ExecuteActions(context, startCallback, cancellationTokenSource.Token));

                Assert.Equal(1, actionExecutionException.ActionIndex);

                Assert.Equal(string.Format(Strings.ErrorMessage_NonzeroExitCode, "1"), actionExecutionException.Message);

                if (waitForCompletion)
                {
                    // The action failure occurs in completion and the actions were specified to have
                    // to wait for completion before executing the next action, thus the start callback
                    // should not have been invoked.
                    Assert.False(calledStartCallback, "Expected start callback to not have been invoked.");
                }
                else
                {
                    // The action failure occurs in completion but all actions were started, thus
                    // the start callback should have been invoked.
                    Assert.True(calledStartCallback, "Expected start callback to have been invoked.");
                }
            });
        }

        [Fact]
        public Task ActionListExecutor_FirstActionFail_DeferredCompletion()
        {
            return ActionListExecutor_FirstActionFail(waitForCompletion: false);
        }

        [Fact]
        public Task ActionListExecutor_FirstActionFail_WaitedCompletion()
        {
            return ActionListExecutor_FirstActionFail(waitForCompletion: true);
        }

        private async Task ActionListExecutor_FirstActionFail(bool waitForCompletion)
        {
            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddExecuteActionAppAction(waitForCompletion, new string[] { "NonzeroExitCode" })
                    .AddExecuteActionAppAction(waitForCompletion, new string[] { "ZeroExitCode" })
                    .SetStartupTrigger();
            }, async host =>
            {
                ActionListExecutor executor = host.Services.GetService<ActionListExecutor>();

                using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

                CollectionRuleOptions ruleOptions = host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>().Get(DefaultRuleName);
                ILogger<CollectionRuleService> logger = host.Services.GetRequiredService<ILogger<CollectionRuleService>>();

                CollectionRuleContext context = new(DefaultRuleName, ruleOptions, null, logger);

                bool calledStartCallback = false;
                Action startCallback = () => calledStartCallback = true;

                CollectionRuleActionExecutionException actionExecutionException = await Assert.ThrowsAsync<CollectionRuleActionExecutionException>(
                    () => executor.ExecuteActions(context, startCallback, cancellationTokenSource.Token));

                Assert.Equal(0, actionExecutionException.ActionIndex);

                Assert.Equal(string.Format(Strings.ErrorMessage_NonzeroExitCode, "1"), actionExecutionException.Message);

                if (waitForCompletion)
                {
                    // The action failure occurs in completion and the actions were specified to have
                    // to wait for completion before executing the next action, thus the start callback
                    // should not have been invoked.
                    Assert.False(calledStartCallback, "Expected start callback to not have been invoked.");
                }
                else
                {
                    // The action failure occurs in completion but all actions were started, thus
                    // the start callback should have been invoked.
                    Assert.True(calledStartCallback, "Expected start callback to have been invoked.");
                }
            });
        }
    }
}