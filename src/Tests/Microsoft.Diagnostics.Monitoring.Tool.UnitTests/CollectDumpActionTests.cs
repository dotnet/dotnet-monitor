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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class CollectDumpActionTests
    {
        private static readonly TimeSpan GetEndpointInfoTimeout = TimeSpan.FromSeconds(10);

        private const int TokenTimeoutMs = 60000; // Arbitrarily set to 1 minute -> potentially needs to be bigger...?
        //private const int DelayMs = 1000;

        private IServiceProvider _serviceProvider;
        private ILogger<CollectDumpAction> _logger;
        private ITestOutputHelper _outputHelper;

        public CollectDumpActionTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            SetUpHost();
        }

        internal void SetUpHost()
        {
            IHost host = new HostBuilder()
                .ConfigureAppConfiguration((IConfigurationBuilder builder) =>
                {
                    builder.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        {ConfigurationPath.Combine(ConfigurationKeys.Storage, nameof(StorageOptions.DumpTempFolder)), StorageOptionsDefaults.DumpTempFolder }
                    });
                })
                .ConfigureServices((HostBuilderContext context, IServiceCollection services) =>
                {
                    services.AddSingleton<EgressOperationQueue>();
                    services.AddSingleton<EgressOperationStore>();
                    services.AddHostedService<EgressOperationService>();

                    services.AddSingleton<RequestLimitTracker>();

                    services.AddSingleton<WebApi.IEndpointInfoSource, FilteredEndpointInfoSource>();
                    services.AddHostedService<FilteredEndpointInfoSourceHostedService>();
                    services.AddSingleton<IDiagnosticServices, DiagnosticServices>();
                    services.ConfigureCollectionRules();
                    services.ConfigureEgress();
                    services.ConfigureOperationStore();
                    services.ConfigureMetrics(context.Configuration);
                    services.ConfigureStorage(context.Configuration);
                    services.ConfigureDefaultProcess(context.Configuration);

                })
                .Build();

            _serviceProvider = host.Services;
            _logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger<CollectDumpAction>();
            //_outputHelper = host.Services.GetService<ITestOutputHelper>();
        }


        [Fact]
        public async Task CollectDumpAction_NoEgressProvider()
        {
            CollectDumpAction action = new(_logger, _serviceProvider);

            CollectDumpOptions options = new();

            options.Egress = null;
            //options.Type = WebApi.Models.DumpType.Full;

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

            ///////////////////

            ServerEndpointInfoCallback callback = new(_outputHelper);
            await using var source = CreateServerSource(out string transportName, callback);
            source.Start();

            var endpointInfos = await GetEndpointInfoAsync(source);
            Assert.Empty(endpointInfos);

            AppRunner runner = CreateAppRunner(transportName, TargetFrameworkMoniker.Net60); // Arbitrarily chose Net60

            Task newEndpointInfoTask = callback.WaitForNewEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);

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

        private ServerEndpointInfoSource CreateServerSource(out string transportName, ServerEndpointInfoCallback callback = null)
        {
            DiagnosticPortHelper.Generate(DiagnosticPortConnectionMode.Listen, out _, out transportName);
            _outputHelper.WriteLine("Starting server endpoint info source at '" + transportName + "'.");

            List<IEndpointInfoSourceCallbacks> callbacks = new();
            if (null != callback)
            {
                callbacks.Add(callback);
            }
            return new ServerEndpointInfoSource(transportName, callbacks);
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
