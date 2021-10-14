// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.Tool.UnitTests.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class ActionListExecutorTests
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

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

                using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(DefaultTimeout);

                CollectionRuleOptions ruleOptions = host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>().Get(DefaultRuleName);
                ILogger<CollectionRuleService> logger = host.Services.GetRequiredService<ILogger<CollectionRuleService>>();

                CollectionRuleContext context = new(DefaultRuleName, ruleOptions, null, logger);

                int callbackCount = 0;
                Action startCallback = () => callbackCount++;

                await executor.ExecuteActions(context, startCallback, cancellationTokenSource.Token);

                VerifyStartCallbackCount(waitForCompletion: false, callbackCount);
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

                using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(DefaultTimeout);

                CollectionRuleOptions ruleOptions = host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>().Get(DefaultRuleName);
                ILogger<CollectionRuleService> logger = host.Services.GetRequiredService<ILogger<CollectionRuleService>>();

                CollectionRuleContext context = new(DefaultRuleName, ruleOptions, null, logger);

                int callbackCount = 0;
                Action startCallback = () => callbackCount++;

                CollectionRuleActionExecutionException actionExecutionException = await Assert.ThrowsAsync<CollectionRuleActionExecutionException>(
                    () => executor.ExecuteActions(context, startCallback, cancellationTokenSource.Token));

                Assert.Equal(1, actionExecutionException.ActionIndex);

                Assert.Equal(string.Format(Strings.ErrorMessage_NonzeroExitCode, "1"), actionExecutionException.Message);

                VerifyStartCallbackCount(waitForCompletion, callbackCount);
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

                using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(DefaultTimeout);

                CollectionRuleOptions ruleOptions = host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>().Get(DefaultRuleName);
                ILogger<CollectionRuleService> logger = host.Services.GetRequiredService<ILogger<CollectionRuleService>>();

                CollectionRuleContext context = new(DefaultRuleName, ruleOptions, null, logger);

                int callbackCount = 0;
                Action startCallback = () => callbackCount++;

                CollectionRuleActionExecutionException actionExecutionException = await Assert.ThrowsAsync<CollectionRuleActionExecutionException>(
                    () => executor.ExecuteActions(context, startCallback, cancellationTokenSource.Token));

                Assert.Equal(0, actionExecutionException.ActionIndex);

                Assert.Equal(string.Format(Strings.ErrorMessage_NonzeroExitCode, "1"), actionExecutionException.Message);

                VerifyStartCallbackCount(waitForCompletion, callbackCount);
            });
        }

        [Fact]
        public async Task ActionListExecutor_Dependencies()
        {
            const string Output1 = nameof(Output1);
            const string Output2 = nameof(Output2);
            const string Output3 = nameof(Output3);

            string a2input1 = FormattableString.Invariant($"$(Actions.a1.{Output1}) with $(Actions.a1.{Output2})");
            string a2input2 = FormattableString.Invariant($"$(Actions.a1.{Output2})");
            string a2input3 = FormattableString.Invariant($"Output $(Actions.a1.{Output3})");

            PassThroughOptions a2Settings = null;

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                CollectionRuleOptions options = rootOptions.CreateCollectionRule(DefaultRuleName);
                options.AddPassThroughAction("a1", "a1input1", "a1input2", "a1input3");
                a2Settings = (PassThroughOptions)options.AddPassThroughAction("a2", a2input1, a2input2, a2input3).Actions.Last().Settings;
                options.SetStartupTrigger();
            }, async host =>
            {
                ActionListExecutor executor = host.Services.GetService<ActionListExecutor>();

                using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(DefaultTimeout);

                CollectionRuleOptions ruleOptions = host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>().Get(DefaultRuleName);
                ILogger<CollectionRuleService> logger = host.Services.GetRequiredService<ILogger<CollectionRuleService>>();

                CollectionRuleContext context = new(DefaultRuleName, ruleOptions, null, logger);

                int callbackCount = 0;
                Action startCallback = () => callbackCount++;

                await executor.ExecuteActions(context, startCallback, cancellationTokenSource.Token);

                //Verify that the original settings were not altered during execution.
                Assert.Equal(a2input1, a2Settings.Input1);
                Assert.Equal(a2input2, a2Settings.Input2);
                Assert.Equal(a2input3, a2Settings.Input3);

                Assert.Equal(1, callbackCount);
                Assert.Equal(2, context.ActionResults.Count);
                Assert.True(context.ActionResults.TryGetValue("a2", out CollectionRuleActionResult a2result));
                Assert.Equal(3, a2result.OutputValues.Count);

                Assert.True(a2result.OutputValues.TryGetValue(Output1, out string a2output1));
                Assert.Equal("a1input1 with a1input2", a2output1);
                Assert.True(a2result.OutputValues.TryGetValue(Output2, out string a2output2));
                Assert.Equal("a1input2", a2output2);
                Assert.True(a2result.OutputValues.TryGetValue(Output3, out string a2output3));
                Assert.Equal("Output a1input3", a2output3);
            }, serviceCollection =>
            {
                serviceCollection.RegisterCollectionRuleAction<PassThroughActionFactory, PassThroughOptions>(nameof(PassThroughAction));
            });
        }

        private static void VerifyStartCallbackCount(bool waitForCompletion, int callbackCount)
        {
            if (waitForCompletion)
            {
                // The action failure occurs in completion and the actions were specified to have
                // to wait for completion before executing the next action, thus the start callback
                // should not have been invoked.
                Assert.Equal(0, callbackCount);
            }
            else
            {
                // The action failure occurs in completion but all actions were started, thus
                // the start callback should have been invoked once.
                Assert.Equal(1, callbackCount);
            }
        }
    }
}