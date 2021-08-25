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
using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class ActionListExecutorTests
    {
        private const int TokenTimeoutMs = 10000;
        private ITestOutputHelper _outputHelper;
        private IServiceProvider _serviceProvider;

        private ILogger<ActionListExecutor> _logger = new Logger<ActionListExecutor>(new LoggerFactory());

        public ActionListExecutorTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;

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
                ValidateActionResult(result, "0");
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

            CollectionRuleActionExecutionException invalidOperationException = await Assert.ThrowsAsync<CollectionRuleActionExecutionException>(
                () => executor.ExecuteActions(collectionRuleActionOptions, null, cancellationTokenSource.Token));

            Assert.Equal(1, invalidOperationException.ActionIndex);

            Assert.Contains(string.Format(Strings.ErrorMessage_NonzeroExitCode, "1"), invalidOperationException.Message);
        }

        [Fact]
        public async Task ActionListExecutor_MultipleExecute_NonZero_Zero()
        {
            ActionListExecutor executor = new(_logger, _serviceProvider);

            CollectionRuleActionOptions actionOptions1 = ConfigureExecuteActionOptions(new string[] { "NonzeroExitCode" });

            CollectionRuleActionOptions actionOptions2 = ConfigureExecuteActionOptions(new string[] { "ZeroExitCode" });

            List<CollectionRuleActionOptions> collectionRuleActionOptions = new() { actionOptions1, actionOptions2 };

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

            CollectionRuleActionExecutionException invalidOperationException = await Assert.ThrowsAsync<CollectionRuleActionExecutionException>(
                () => executor.ExecuteActions(collectionRuleActionOptions, null, cancellationTokenSource.Token));

            Assert.Equal(0, invalidOperationException.ActionIndex);

            Assert.Contains(string.Format(Strings.ErrorMessage_NonzeroExitCode, "1"), invalidOperationException.Message);
        }

        private static CollectionRuleActionOptions ConfigureExecuteActionOptions(string[] args, string customPath = null)
        {
            CollectionRuleActionOptions actionOptions = new();

            actionOptions.Type = KnownCollectionRuleActions.Execute;

            ExecuteOptions options = new();

            options.Path = (customPath != null) ? customPath : DotNetHost.HostExePath;
            options.Arguments = GenerateArgumentsString(args);

            actionOptions.Settings = options;

            return actionOptions;
        }

        private static string GenerateArgumentsString(string[] additionalArgs)
        {
            Assembly currAssembly = Assembly.GetExecutingAssembly();

            List<string> args = new();

            // Entrypoint assembly
            args.Add(AssemblyHelper.GetAssemblyArtifactBinPath(currAssembly, "Microsoft.Diagnostics.Monitoring.ExecuteActionApp", TargetFrameworkMoniker.NetCoreApp31));

            // Entrypoint arguments
            args.AddRange(additionalArgs);

            return string.Join(' ', args);
        }

        private static void ValidateActionResult(CollectionRuleActionResult result, string expectedExitCode)
        {
            string actualExitCode;

            Assert.NotNull(result.OutputValues);
            Assert.True(result.OutputValues.TryGetValue("ExitCode", out actualExitCode));
            Assert.Equal(expectedExitCode, actualExitCode);
        }
    }
}