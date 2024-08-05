// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CollectionRuleActions.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(TestCollections.CollectionRuleActions)]
    public sealed class ActionListExecutorTests
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

        private readonly ITestOutputHelper _outputHelper;

        private const string DefaultRuleName = nameof(ActionListExecutorTests);

        private static Microsoft.Diagnostics.Monitoring.WebApi.IProcessInfo CreateTestProcess()
        {
            Mock<Microsoft.Diagnostics.Monitoring.WebApi.IProcessInfo> mockProcessInfo = new();
            mockProcessInfo
                .Setup(p => p.EndpointInfo)
                .Returns(Mock.Of<Microsoft.Diagnostics.Monitoring.WebApi.IEndpointInfo>);

            return mockProcessInfo.Object;
        }

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
                    .AddExecuteActionAppAction(Assembly.GetExecutingAssembly(), ActionTestsConstants.ZeroExitCode)
                    .AddExecuteActionAppAction(Assembly.GetExecutingAssembly(), ActionTestsConstants.ZeroExitCode)
                    .SetStartupTrigger();
            }, async host =>
            {
                ActionListExecutor executor = host.Services.GetService<ActionListExecutor>();

                using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(DefaultTimeout);

                CollectionRuleOptions ruleOptions = host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>().Get(DefaultRuleName);
                ILogger<CollectionRuleService> logger = host.Services.GetRequiredService<ILogger<CollectionRuleService>>();
                TimeProvider timeProvider = host.Services.GetRequiredService<TimeProvider>();

                CollectionRuleContext context = new(DefaultRuleName, ruleOptions, CreateTestProcess(), HostInfo.GetCurrent(timeProvider), logger);

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
                    .AddExecuteActionAppAction(Assembly.GetExecutingAssembly(), waitForCompletion, ActionTestsConstants.ZeroExitCode)
                    .AddExecuteActionAppAction(Assembly.GetExecutingAssembly(), waitForCompletion, ActionTestsConstants.NonZeroExitCode)
                    .SetStartupTrigger();
            }, async host =>
            {
                ActionListExecutor executor = host.Services.GetService<ActionListExecutor>();

                using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(DefaultTimeout);

                CollectionRuleOptions ruleOptions = host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>().Get(DefaultRuleName);
                ILogger<CollectionRuleService> logger = host.Services.GetRequiredService<ILogger<CollectionRuleService>>();
                TimeProvider timeProvider = host.Services.GetRequiredService<TimeProvider>();

                CollectionRuleContext context = new(DefaultRuleName, ruleOptions, CreateTestProcess(), HostInfo.GetCurrent(timeProvider), logger);

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
                    .AddExecuteActionAppAction(Assembly.GetExecutingAssembly(), waitForCompletion, ActionTestsConstants.NonZeroExitCode)
                    .AddExecuteActionAppAction(Assembly.GetExecutingAssembly(), waitForCompletion, ActionTestsConstants.ZeroExitCode)
                    .SetStartupTrigger();
            }, async host =>
            {
                ActionListExecutor executor = host.Services.GetService<ActionListExecutor>();

                using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(DefaultTimeout);

                CollectionRuleOptions ruleOptions = host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>().Get(DefaultRuleName);
                ILogger<CollectionRuleService> logger = host.Services.GetRequiredService<ILogger<CollectionRuleService>>();
                TimeProvider timeProvider = host.Services.GetRequiredService<TimeProvider>();

                CollectionRuleContext context = new(DefaultRuleName, ruleOptions, CreateTestProcess(), HostInfo.GetCurrent(timeProvider), logger);

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
        public async Task DuplicateActionNamesTest()
        {
            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                CollectionRuleOptions options = rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddPassThroughAction("a1", "a1input1", "a1input2", "a1input3")
                    .AddPassThroughAction("a2", "a1input1", "a1input2", "a1input3")
                    .AddPassThroughAction("a1", "a1input1", "a1input2", "a1input3")
                    .SetStartupTrigger();
            }, host =>
            {
                //Expecting duplicate action name
                Assert.Throws<OptionsValidationException>(() => host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>().Get(DefaultRuleName));
            }, serviceCollection =>
            {
                serviceCollection.RegisterCollectionRuleAction<PassThroughActionFactory, PassThroughOptions>(nameof(PassThroughAction));
            });
        }

        private static void VerifyStartCallbackCount(bool waitForCompletion, int callbackCount)
        {
            //Currently, any attempt to wait on completion will automatically trigger the start callback.
            //This is necessary to ensure that the process is resumed prior to completing artifact collection.
            Assert.Equal(1, callbackCount);
        }
    }
}
