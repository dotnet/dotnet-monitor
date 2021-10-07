﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class ActionTester<T>
    {
        private IHost _host;
        private EndpointUtilities _endpointUtilities;
        private ITestOutputHelper _outputHelper;

        internal ActionTester(IHost host, EndpointUtilities endpointUtilities, ITestOutputHelper outputHelper)
        {
            _endpointUtilities = endpointUtilities;
            _host = host;
            _outputHelper = outputHelper;
        }

        internal async Task TestAction(string ruleName, string actionName, TimeSpan actionTimeout, Func<string, AppRunner, Task> actionValidation, TargetFrameworkMoniker tfm)
        {
            IOptionsMonitor<CollectionRuleOptions> ruleOptionsMonitor = _host.Services.GetService<IOptionsMonitor<CollectionRuleOptions>>();
            T options = (T)ruleOptionsMonitor.Get(ruleName).Actions[0].Settings;

            ICollectionRuleActionFactoryProxy factory;
            Assert.True(_host.Services.GetService<ICollectionRuleActionOperations>().TryCreateFactory(actionName, out factory));

            EndpointInfoSourceCallback callback = new(_outputHelper);
            await using var source = _endpointUtilities.CreateServerSource(out string transportName, callback);
            source.Start();

            AppRunner runner = _endpointUtilities.CreateAppRunner(transportName, tfm);

            Task<IEndpointInfo> newEndpointInfoTask = callback.WaitForNewEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);

            await runner.ExecuteAsync(async () =>
            {
                IEndpointInfo endpointInfo = await newEndpointInfoTask;

                ICollectionRuleAction action = factory.Create(endpointInfo, options);

                using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(actionTimeout);

                CollectionRuleActionResult result;
                try
                {
                    await action.StartAsync(cancellationTokenSource.Token);

                    result = await action.WaitForCompletionAsync(cancellationTokenSource.Token);
                }
                finally
                {
                    await DisposableHelper.DisposeAsync(action);
                }

                string egressPath = ValidateEgressPath(result);

                await actionValidation(egressPath, runner);

                // Need to ask Justin why this is the case...just a workaround right now, but don't understand what this does and why it has different behavior by Action
                if (!actionName.Equals(KnownCollectionRuleActions.CollectDump))
                {
                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                }
            });
        }

        private static string ValidateEgressPath(CollectionRuleActionResult result)
        {
            Assert.NotNull(result.OutputValues);
            Assert.True(result.OutputValues.TryGetValue(CollectionRuleActionConstants.EgressPathOutputValueName, out string egressPath));
            Assert.True(File.Exists(egressPath));

            return egressPath;
        }
    }
}