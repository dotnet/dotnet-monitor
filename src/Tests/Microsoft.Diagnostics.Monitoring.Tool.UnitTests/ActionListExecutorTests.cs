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

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class ActionListExecutorTests
    {
        private const int TokenTimeoutMs = 10000;
        // private const int DelayMs = 1000;

        // Not sure how to hook into Services for Singleton executor and logger, so currently just making one for the tests
        private ILogger<ActionListExecutor> _logger = new Logger<ActionListExecutor>(new LoggerFactory());
        
        //private ActionListExecutor _executor;

        /*
         * Experimented with adding in Fixtures similar to FunctionalTests
         * 
        public ActionListExecutorTests(ServiceProviderFixture serviceProviderFixture)
        {
            _executor = serviceProviderFixture.ServiceProvider.GetService<ActionListExecutor>();
        }*/

        [Fact]
        public async Task ActionListExecutor_MultipleExecute_Zero_Zero()
        {
            ActionListExecutor executor = new(_logger); // Not using as singleton in tests...is this acceptable, or should I not be instantiating for each test?

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
            ActionListExecutor executor = new(_logger); // Not using as singleton in tests...is this acceptable, or should I not be instantiating for each test?

            CollectionRuleActionOptions actionOptions1 = ConfigureExecuteActionOptions(new string[] { "ZeroExitCode" });

            CollectionRuleActionOptions actionOptions2 = ConfigureExecuteActionOptions(new string[] { "NonzeroExitCode" });

            List<CollectionRuleActionOptions> collectionRuleActionOptions = new() { actionOptions1, actionOptions2 };

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

            List<CollectionRuleActionResult> results = await executor.ExecuteActions(collectionRuleActionOptions, null, cancellationTokenSource.Token);

            ValidateActionResult(results[0], "0");

            Assert.Single(results);
        }

        [Fact]
        public async Task ActionListExecutor_MultipleExecute_NonZero_Zero()
        {
            ActionListExecutor executor = new(_logger); // Not using as singleton in tests...is this acceptable, or should I not be instantiating for each test?

            CollectionRuleActionOptions actionOptions1 = ConfigureExecuteActionOptions(new string[] { "NonzeroExitCode" });

            CollectionRuleActionOptions actionOptions2 = ConfigureExecuteActionOptions(new string[] { "ZeroExitCode" });

            List<CollectionRuleActionOptions> collectionRuleActionOptions = new() { actionOptions1, actionOptions2 };

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

            List<CollectionRuleActionResult> results = await executor.ExecuteActions(collectionRuleActionOptions, null, cancellationTokenSource.Token);

            Assert.Empty(results);
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