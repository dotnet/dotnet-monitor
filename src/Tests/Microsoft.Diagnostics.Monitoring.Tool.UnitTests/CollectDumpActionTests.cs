// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using System.Threading;
using System;
using System.IO;
using System.Globalization;
using Xunit.Abstractions;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using System.Collections.Generic;
using static Microsoft.Diagnostics.Monitoring.Tool.UnitTests.EndpointInfoSourceTests;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Tools.Monitor;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class CollectDumpActionTests
    {
        private static readonly TimeSpan GetEndpointInfoTimeout = TimeSpan.FromSeconds(10);

        private const int TokenTimeoutMs = 60000; // Arbitrarily set to 1 minute -> potentially needs to be bigger...?
        //private const int DelayMs = 1000;


        //private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;
        private readonly IServiceProvider _serviceProvider;
        // Not sure how to hook into Services for Singleton executor and logger, so currently just making one for the tests
        private ILogger<CollectDumpAction> _logger = new Logger<CollectDumpAction>(new LoggerFactory());

        public CollectDumpActionTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            //_httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
            _serviceProvider = serviceProviderFixture.ServiceProvider;
        }

        [Fact]
        public async Task CollectDumpAction_NoEgressProvider()
        {
            CollectDumpAction action = new(_logger, _serviceProvider);

            CollectDumpOptions options = new();

            options.Egress = null;

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);


            ///////////////////

            await using var source = CreateServerSource(out string transportName);
            source.Start();

            var endpointInfos = await GetEndpointInfoAsync(source);
            Assert.Empty(endpointInfos);

            AppRunner runner = CreateAppRunner(transportName, TargetFrameworkMoniker.Net60); // Arbitrarily chose Net60

            Task newEndpointInfoTask = source.WaitForNewEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);

            await runner.ExecuteAsync(async () =>
            {
                await newEndpointInfoTask;

                endpointInfos = await GetEndpointInfoAsync(source);

                var endpointInfo = Assert.Single(endpointInfos);
                Assert.NotNull(endpointInfo.CommandLine);
                Assert.NotNull(endpointInfo.OperatingSystem);
                Assert.NotNull(endpointInfo.ProcessArchitecture);
                VerifyConnection(runner, endpointInfo);

                CollectionRuleActionResult result = await action.ExecuteAsync(options, endpointInfo, cancellationTokenSource.Token);

                string egressPath = result.OutputValues["EgressPath"];

                if (!File.Exists(egressPath))
                {
                    throw new FileNotFoundException(string.Format(CultureInfo.InvariantCulture, Tools.Monitor.Strings.ErrorMessage_FileNotFound, egressPath));
                }

                await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
            });

            await Task.Delay(TimeSpan.FromSeconds(1));

            endpointInfos = await GetEndpointInfoAsync(source);

            Assert.Empty(endpointInfos);


            ////////////////
            ///
            /*

            CollectionRuleActionResult result = await action.ExecuteAsync(options, endpointInfo, cancellationTokenSource.Token);

            string egressPath = result.OutputValues["EgressPath"];

            if (!File.Exists(egressPath))
            {
                throw new FileNotFoundException(string.Format(CultureInfo.InvariantCulture, Tools.Monitor.Strings.ErrorMessage_FileNotFound, egressPath));
            }

            //ValidateActionResult(result, "0");
            */
        }


        public static IEnumerable<object[]> GetTfmsSupportingPortListener()
        {
            yield return new object[] { TargetFrameworkMoniker.Net50 };
            yield return new object[] { TargetFrameworkMoniker.Net60 };
        }

        private TestServerEndpointInfoSource CreateServerSource(out string transportName)
        {
            DiagnosticPortHelper.Generate(DiagnosticPortConnectionMode.Listen, out _, out transportName);
            _outputHelper.WriteLine("Starting server endpoint info source at '" + transportName + "'.");
            return new TestServerEndpointInfoSource(transportName, _outputHelper);
        }

        private AppRunner CreateAppRunner(string transportName, TargetFrameworkMoniker tfm, int appId = 1)
        {
            AppRunner appRunner = new(_outputHelper, Assembly.GetExecutingAssembly(), appId, tfm);
            appRunner.ConnectionMode = DiagnosticPortConnectionMode.Connect;
            appRunner.DiagnosticPortPath = transportName;
            appRunner.ScenarioName = TestAppScenarios.AsyncWait.Name;
            return appRunner;
        }

        private async Task<IEnumerable<IEndpointInfo>> GetEndpointInfoAsync(ServerEndpointInfoSource source)
        {
            _outputHelper.WriteLine("Getting endpoint infos.");
            using CancellationTokenSource cancellationSource = new(GetEndpointInfoTimeout);
            return await source.GetEndpointInfoAsync(cancellationSource.Token);
        }

        /// <summary>
        /// Verifies basic information on the connection and that it matches the target process from the runner.
        /// </summary>
        private static void VerifyConnection(AppRunner runner, IEndpointInfo endpointInfo)
        {
            Assert.NotNull(runner);
            Assert.NotNull(endpointInfo);
            Assert.Equal(runner.ProcessId, endpointInfo.ProcessId);
            Assert.NotEqual(Guid.Empty, endpointInfo.RuntimeInstanceCookie);
            Assert.NotNull(endpointInfo.Endpoint);
        }

    }
}
