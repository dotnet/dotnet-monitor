// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal sealed class EndpointUtilities
    {
        private readonly ITestOutputHelper _outputHelper;

        private static readonly TimeSpan GetEndpointInfoTimeout = TimeSpan.FromSeconds(10);

        public EndpointUtilities(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        public async Task<ServerSourceHolder> StartServerAsync(EndpointInfoSourceCallback sourceCallback = null, IDumpService dumpService = null,
            OperationTrackerService operationTrackerService = null)
        {
            DiagnosticPortHelper.Generate(DiagnosticPortConnectionMode.Listen, out _, out string transportName);
            _outputHelper.WriteLine("Starting server endpoint info source at '" + transportName + "'.");

            List<IEndpointInfoSourceCallbacks> callbacks = new();
            if (null != sourceCallback)
            {
                callbacks.Add(sourceCallback);
                if (null != dumpService)
                {
                    callbacks.Add(new OperationTrackerServiceEndpointInfoSourceCallback(operationTrackerService));
                }
            }

            IOptions<DiagnosticPortOptions> portOptions = Extensions.Options.Options.Create(
                new DiagnosticPortOptions()
                {
                    ConnectionMode = DiagnosticPortConnectionMode.Listen,
                    EndpointName = transportName
                });

            ScopedEndpointInfo scopedEndpointInfo = new();

            Mock<IServiceProvider> scopedServiceProviderMock = new Mock<IServiceProvider>();
            scopedServiceProviderMock
                .Setup(sp => sp.GetService(typeof(ScopedEndpointInfo)))
                .Returns(scopedEndpointInfo);
            scopedServiceProviderMock
                .Setup(sp => sp.GetService(typeof(IEnumerable<IDiagnosticLifetimeService>)))
                .Returns(Enumerable.Empty<IDiagnosticLifetimeService>());

            Mock<IServiceScope> serviceScopeMock = new Mock<IServiceScope>();
            serviceScopeMock
                .Setup(scope => scope.ServiceProvider)
                .Returns(scopedServiceProviderMock.Object);

            Mock<IServiceScopeFactory> scopeFactoryMock = new Mock<IServiceScopeFactory>();
            scopeFactoryMock
                .Setup(factory => factory.CreateScope())
                .Returns(serviceScopeMock.Object);

            IServerEndpointStateChecker endpointChecker = new ServerEndpointStateChecker(operationTrackerService);

            ServerEndpointTracker endpointTracker = new ServerEndpointTracker(endpointChecker, portOptions);

            ServerEndpointInfoSource source = new(
                scopeFactoryMock.Object,
                endpointTracker,
                portOptions,
                NullLogger<ServerEndpointInfoSource>.Instance,
                callbacks);

            await Task.WhenAll(
                endpointTracker.StartAsync(CancellationToken.None),
                source.StartAsync(CancellationToken.None)
                );

            return new ServerSourceHolder(source, transportName);
        }

        public AppRunner CreateAppRunner(Assembly testAssembly, string transportName, TargetFrameworkMoniker tfm, int appId = 1)
        {
            AppRunner appRunner = new(_outputHelper, testAssembly, appId, tfm);
            appRunner.ConnectionMode = DiagnosticPortConnectionMode.Connect;
            appRunner.DiagnosticPortPath = transportName;
            appRunner.ScenarioName = TestAppScenarios.AsyncWait.Name;
            return appRunner;
        }

        public async Task<IEnumerable<IEndpointInfo>> GetEndpointInfoAsync(ServerEndpointInfoSource source)
        {
            _outputHelper.WriteLine("Getting endpoint infos.");
            using CancellationTokenSource cancellationSource = new(GetEndpointInfoTimeout);
            return await source.GetEndpointInfoAsync(cancellationSource.Token);
        }

        /// <summary>
        /// Verifies basic information on the connection and that it matches the target process from the runner.
        /// </summary>
        public static async Task VerifyConnectionAsync(AppRunner runner, IEndpointInfo endpointInfo)
        {
            Assert.NotNull(runner);
            Assert.NotNull(endpointInfo);
            Assert.Equal(await runner.ProcessIdTask, endpointInfo.ProcessId);
            Assert.NotEqual(Guid.Empty, endpointInfo.RuntimeInstanceCookie);
            Assert.NotNull(endpointInfo.Endpoint);
        }
    }

    internal sealed class ServerSourceHolder : IAsyncDisposable
    {
        public ServerSourceHolder(ServerEndpointInfoSource source, string transportName)
        {
            Source = source;
            TransportName = transportName;
        }

        public ServerEndpointInfoSource Source { get; }

        public string TransportName { get; }

        public async ValueTask DisposeAsync()
        {
            await Source.StopAsync(CancellationToken.None);

            Source.Dispose();
        }
    }
}
