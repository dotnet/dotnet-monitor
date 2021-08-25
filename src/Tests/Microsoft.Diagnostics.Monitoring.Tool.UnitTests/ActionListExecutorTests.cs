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
using System.Collections.Generic;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class ActionListExecutorTests
    {
        private const int TokenTimeoutMs = 10000;

        private IServiceProvider _serviceProvider;
        private ILogger<ActionListExecutor> _logger;

        public ActionListExecutorTests()
        {
            SetUpHost();
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

            _serviceProvider = host.Services;
            _logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger<ActionListExecutor>();
        }

        [Fact]
        public async Task ActionListExecutor_MultipleExecute_Zero_Zero()
        {
            ActionListExecutor executor = new(_logger, _serviceProvider);

            CollectionRuleActionOptions actionOptions1 = ConfigureExecuteActionOptions(new string[] { "ZeroExitCode"});

            CollectionRuleActionOptions actionOptions2 = ConfigureExecuteActionOptions(new string[] { "ZeroExitCode" });

            List<CollectionRuleActionOptions> collectionRuleActionOptions = new() { actionOptions1, actionOptions2 };

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

            List<CollectionRuleActionResult> results = await executor.ExecuteActions(collectionRuleActionOptions, null, cancellationTokenSource.Token);

            foreach (var result in results)
            {
                ExecuteActionTests.ValidateActionResult(result, "0");
            }
        }

        [Fact]
        public async Task ActionListExecutor_MultipleExecute_Zero_Nonzero()
        {
            ActionListExecutor executor = new(_logger, _serviceProvider);

            CollectionRuleActionOptions actionOptions1 = ConfigureExecuteActionOptions(new string[] { "ZeroExitCode" });

            CollectionRuleActionOptions actionOptions2 = ConfigureExecuteActionOptions(new string[] { "NonzeroExitCode" });

            List<CollectionRuleActionOptions> collectionRuleActionOptions = new() { actionOptions1, actionOptions2 };

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

            CollectionRuleActionExecutionException actionExecutionException = await Assert.ThrowsAsync<CollectionRuleActionExecutionException>(
                () => executor.ExecuteActions(collectionRuleActionOptions, null, cancellationTokenSource.Token));

            Assert.Equal(1, actionExecutionException.ActionIndex);

            Assert.Contains(string.Format(Strings.ErrorMessage_NonzeroExitCode, "1"), actionExecutionException.Message);
        }

        [Fact]
        public async Task ActionListExecutor_MultipleExecute_NonZero_Zero()
        {
            ActionListExecutor executor = new(_logger, _serviceProvider);

            CollectionRuleActionOptions actionOptions1 = ConfigureExecuteActionOptions(new string[] { "NonzeroExitCode" });

            CollectionRuleActionOptions actionOptions2 = ConfigureExecuteActionOptions(new string[] { "ZeroExitCode" });

            List<CollectionRuleActionOptions> collectionRuleActionOptions = new() { actionOptions1, actionOptions2 };

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

            CollectionRuleActionExecutionException actionExecutionException = await Assert.ThrowsAsync<CollectionRuleActionExecutionException>(
                () => executor.ExecuteActions(collectionRuleActionOptions, null, cancellationTokenSource.Token));

            Assert.Equal(0, actionExecutionException.ActionIndex);

            Assert.Contains(string.Format(Strings.ErrorMessage_NonzeroExitCode, "1"), actionExecutionException.Message);
        }

        private static CollectionRuleActionOptions ConfigureExecuteActionOptions(string[] args, string customPath = null)
        {
            CollectionRuleActionOptions actionOptions = new();

            actionOptions.Type = KnownCollectionRuleActions.Execute;

            ExecuteOptions options = new();

            options.Path = (customPath != null) ? customPath : DotNetHost.HostExePath;
            options.Arguments = ExecuteActionTests.GenerateArgumentsString(args);

            actionOptions.Settings = options;

            return actionOptions;
        }
    }
}