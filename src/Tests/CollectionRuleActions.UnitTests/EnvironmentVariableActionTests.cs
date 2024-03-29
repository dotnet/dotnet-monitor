// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CollectionRuleActions.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(TestCollections.CollectionRuleActions)]
    public class EnvironmentVariableActionTests
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly EndpointUtilities _endpointUtilities;

        private const string DefaultRuleName = "StartupRule";
        private const string DefaultVarValue = "TheValueStoredIn the environment";

        public EnvironmentVariableActionTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _endpointUtilities = new(_outputHelper);
        }

        /// <summary>
        /// Test that the <see cref="SetEnvironmentVariableActionFactory.SetEnvironmentVariableAction"/> works correctly.
        /// </summary>
        /// <remarks>
        /// The required APIs only exist on .NET 6.0+
        /// </remarks>
        [Theory]
        [MemberData(nameof(ActionTestsHelper.Get6PlusTfms), MemberType = typeof(ActionTestsHelper))]
        public async Task TestSetEnvVar(TargetFrameworkMoniker tfm)
        {
            await TestHostHelper.CreateCollectionRulesHost(
                outputHelper: _outputHelper,
                setup: (RootOptions rootOptions) =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddSetEnvironmentVariableAction(TestAppScenarios.EnvironmentVariables.CustomVariableName, DefaultVarValue);
                },
                hostCallback: async (IHost host) =>
                {
                    SetEnvironmentVariableOptions setOpts = ActionTestsHelper.GetActionOptions<SetEnvironmentVariableOptions>(host, DefaultRuleName, 0);

                    ICollectionRuleActionOperations actionOperationsService = host.Services.GetService<ICollectionRuleActionOperations>();
                    Assert.True(actionOperationsService.TryCreateFactory(KnownCollectionRuleActions.SetEnvironmentVariable, out ICollectionRuleActionFactoryProxy setFactory));

                    EndpointInfoSourceCallback endpointInfoCallback = new(_outputHelper);
                    await using ServerSourceHolder sourceHolder = await _endpointUtilities.StartServerAsync(endpointInfoCallback);

                    await using AppRunner runner = _endpointUtilities.CreateAppRunner(Assembly.GetExecutingAssembly(), sourceHolder.TransportName, tfm);
                    runner.ScenarioName = TestAppScenarios.EnvironmentVariables.Name;

                    Task<IProcessInfo> processInfoTask = endpointInfoCallback.WaitAddedProcessInfoAsync(runner, CommonTestTimeouts.StartProcess);
                    await runner.ExecuteAsync(async () =>
                    {
                        IProcessInfo processInfo = await processInfoTask;

                        ICollectionRuleAction setAction = setFactory.Create(processInfo, setOpts);

                        await ActionTestsHelper.ExecuteAndDisposeAsync(setAction, CommonTestTimeouts.EnvVarsTimeout);

                        Assert.Equal(DefaultVarValue, await runner.GetEnvironmentVariable(TestAppScenarios.EnvironmentVariables.CustomVariableName, CommonTestTimeouts.EnvVarsTimeout));

                        await runner.SendCommandAsync(TestAppScenarios.EnvironmentVariables.Commands.ShutdownScenario);
                    });
                });
        }

        /// <summary>
        /// Test that the <see cref="GetEnvironmentVariableActionFactory.GetEnvironmentVariableAction"/> works correctly.
        /// </summary>
        /// <remarks>
        /// The required APIs only exist on .NET 6.0+
        /// </remarks>
        [Theory]
        [MemberData(nameof(ActionTestsHelper.Get6PlusTfms), MemberType = typeof(ActionTestsHelper))]
        public async Task TestGetEnvVar(TargetFrameworkMoniker tfm)
        {
            const string VariableDoesNotExist = "SomeEnvVarThatIsNotSet";
            await TestHostHelper.CreateCollectionRulesHost(
                outputHelper: _outputHelper,
                setup: (RootOptions rootOptions) =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddGetEnvironmentVariableAction(TestAppScenarios.EnvironmentVariables.IncrementVariableName)
                        .AddGetEnvironmentVariableAction(VariableDoesNotExist);
                },
                hostCallback: async (IHost host) =>
                {
                    GetEnvironmentVariableOptions getOpts = ActionTestsHelper.GetActionOptions<GetEnvironmentVariableOptions>(host, DefaultRuleName, 0);
                    GetEnvironmentVariableOptions getFailOpts = ActionTestsHelper.GetActionOptions<GetEnvironmentVariableOptions>(host, DefaultRuleName, 1);

                    ICollectionRuleActionOperations actionOperationsService = host.Services.GetService<ICollectionRuleActionOperations>();
                    Assert.True(actionOperationsService.TryCreateFactory(KnownCollectionRuleActions.GetEnvironmentVariable, out ICollectionRuleActionFactoryProxy getFactory));

                    EndpointInfoSourceCallback endpointInfoCallback = new(_outputHelper);
                    await using ServerSourceHolder sourceHolder = await _endpointUtilities.StartServerAsync(endpointInfoCallback);

                    await using AppRunner runner = _endpointUtilities.CreateAppRunner(Assembly.GetExecutingAssembly(), sourceHolder.TransportName, tfm);
                    runner.ScenarioName = TestAppScenarios.EnvironmentVariables.Name;

                    Task<IProcessInfo> processInfoTask = endpointInfoCallback.WaitAddedProcessInfoAsync(runner, CommonTestTimeouts.StartProcess);

                    await runner.ExecuteAsync(async () =>
                    {
                        IProcessInfo processInfo = await processInfoTask;

                        ICollectionRuleAction getAction = getFactory.Create(processInfo, getOpts);

                        await runner.SendCommandAsync(TestAppScenarios.EnvironmentVariables.Commands.IncVar);
                        Assert.Equal("1", await runner.GetEnvironmentVariable(TestAppScenarios.EnvironmentVariables.IncrementVariableName, CommonTestTimeouts.EnvVarsTimeout));

                        await runner.SendCommandAsync(TestAppScenarios.EnvironmentVariables.Commands.IncVar);
                        Assert.Equal("2", await runner.GetEnvironmentVariable(TestAppScenarios.EnvironmentVariables.IncrementVariableName, CommonTestTimeouts.EnvVarsTimeout));

                        CollectionRuleActionResult result = await ActionTestsHelper.ExecuteAndDisposeAsync(getAction, CommonTestTimeouts.EnvVarsTimeout);
                        Assert.Equal("2", result.OutputValues[CollectionRuleActionConstants.EnvironmentVariableValueName]);

                        await runner.SendCommandAsync(TestAppScenarios.EnvironmentVariables.Commands.IncVar);
                        Assert.Equal("3", await runner.GetEnvironmentVariable(TestAppScenarios.EnvironmentVariables.IncrementVariableName, CommonTestTimeouts.EnvVarsTimeout));

                        ICollectionRuleAction getActionFailure = getFactory.Create(processInfo, getFailOpts);
                        CollectionRuleActionException thrownEx = await Assert.ThrowsAsync<CollectionRuleActionException>(async () =>
                        {
                            await ActionTestsHelper.ExecuteAndDisposeAsync(getActionFailure, CommonTestTimeouts.EnvVarsTimeout);
                        });
                        Assert.Contains(VariableDoesNotExist, thrownEx.Message);

                        await runner.SendCommandAsync(TestAppScenarios.EnvironmentVariables.Commands.ShutdownScenario);
                    });
                });
        }

        /// <summary>
        /// Test that the <see cref="SetEnvironmentVariableActionFactory.SetEnvironmentVariableAction"/> to 
        /// <see cref="GetEnvironmentVariableActionFactory.GetEnvironmentVariableAction"/> round trip works correctly.
        /// </summary>
        /// <remarks>
        /// The required APIs only exist on .NET 6.0+
        /// </remarks>
        [Theory]
        [MemberData(nameof(ActionTestsHelper.Get6PlusTfms), MemberType = typeof(ActionTestsHelper))]
        public async Task TestEnvVarRoundTrip(TargetFrameworkMoniker tfm)
        {
            await TestHostHelper.CreateCollectionRulesHost(
                outputHelper: _outputHelper,
                setup: (RootOptions rootOptions) =>
                {
                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddSetEnvironmentVariableAction(TestAppScenarios.EnvironmentVariables.CustomVariableName, DefaultVarValue)
                        .AddGetEnvironmentVariableAction(TestAppScenarios.EnvironmentVariables.CustomVariableName);
                },
                hostCallback: async (IHost host) =>
                {
                    SetEnvironmentVariableOptions setOpts = ActionTestsHelper.GetActionOptions<SetEnvironmentVariableOptions>(host, DefaultRuleName, 0);
                    GetEnvironmentVariableOptions getOpts = ActionTestsHelper.GetActionOptions<GetEnvironmentVariableOptions>(host, DefaultRuleName, 1);

                    ICollectionRuleActionOperations actionOperationsService = host.Services.GetService<ICollectionRuleActionOperations>();
                    Assert.True(actionOperationsService.TryCreateFactory(KnownCollectionRuleActions.SetEnvironmentVariable, out ICollectionRuleActionFactoryProxy setFactory));
                    Assert.True(actionOperationsService.TryCreateFactory(KnownCollectionRuleActions.GetEnvironmentVariable, out ICollectionRuleActionFactoryProxy getFactory));

                    EndpointInfoSourceCallback endpointInfoCallback = new(_outputHelper);
                    await using ServerSourceHolder sourceHolder = await _endpointUtilities.StartServerAsync(endpointInfoCallback);

                    await using AppRunner runner = _endpointUtilities.CreateAppRunner(Assembly.GetExecutingAssembly(), sourceHolder.TransportName, tfm);
                    runner.ScenarioName = TestAppScenarios.EnvironmentVariables.Name;

                    Task<IProcessInfo> newProcessInfoTask = endpointInfoCallback.WaitAddedProcessInfoAsync(runner, CommonTestTimeouts.StartProcess);

                    await runner.ExecuteAsync(async () =>
                        {
                            IProcessInfo processInfo = await newProcessInfoTask;
                            ICollectionRuleAction setAction = setFactory.Create(processInfo, setOpts);
                            ICollectionRuleAction getAction = getFactory.Create(processInfo, getOpts);

                            await ActionTestsHelper.ExecuteAndDisposeAsync(setAction, CommonTestTimeouts.EnvVarsTimeout);
                            CollectionRuleActionResult getResult = await ActionTestsHelper.ExecuteAndDisposeAsync(getAction, CommonTestTimeouts.EnvVarsTimeout);

                            await runner.SendCommandAsync(TestAppScenarios.EnvironmentVariables.Commands.ShutdownScenario);

                            Assert.True(getResult.OutputValues.TryGetValue(CollectionRuleActionConstants.EnvironmentVariableValueName, out string value));
                            Assert.Equal(DefaultVarValue, value);
                        });
                });
        }
    }
}
