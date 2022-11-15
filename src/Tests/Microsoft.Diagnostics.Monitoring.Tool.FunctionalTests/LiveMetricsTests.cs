// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class LiveMetricsTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public LiveMetricsTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

        [Fact]
        public Task TestDefaultMetrics()
        {
            return ScenarioRunner.SingleTarget(_outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Connect,
                TestAppScenarios.AsyncWait.Name,
                async (appRunner, apiClient) =>
                {
                    using ResponseStreamHolder holder = await apiClient.CaptureMetricsAsync(await appRunner.ProcessIdTask,
                        durationSeconds: 10);

                    var metrics = LiveMetricsTestUtilities.GetAllMetrics(holder.Stream);
                    await LiveMetricsTestUtilities.ValidateMetrics(new[] { EventPipe.MonitoringSourceConfiguration.SystemRuntimeEventSourceName },
                        new[]
                        {
                            "cpu-usage",
                            "working-set",
                            "gc-heap-size",
                            "threadpool-thread-count",
                            "threadpool-queue-length"
                        },
                    metrics, strict: false);

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                });
        }

        [Fact]
        public Task TestCustomMetrics()
        {
            return ScenarioRunner.SingleTarget(_outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Connect,
                TestAppScenarios.AsyncWait.Name,
                async (appRunner, apiClient) =>
                {
                    var counterNames = new[] { "cpu-usage", "working-set" };

                    using ResponseStreamHolder holder = await apiClient.CaptureMetricsAsync(await appRunner.ProcessIdTask,
                        durationSeconds: 10,
                        metricsConfiguration: new EventMetricsConfiguration
                        {
                            IncludeDefaultProviders = false,
                            Providers = new[]
                            {
                                new EventMetricsProvider
                                {
                                    ProviderName = EventPipe.MonitoringSourceConfiguration.SystemRuntimeEventSourceName,
                                    CounterNames = counterNames,
                                }
                            }
                        });

                    var metrics = LiveMetricsTestUtilities.GetAllMetrics(holder.Stream);
                    await LiveMetricsTestUtilities.ValidateMetrics(new[] { EventPipe.MonitoringSourceConfiguration.SystemRuntimeEventSourceName },
                        counterNames,
                        metrics,
                        strict: true);

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                });
        }

#if NET7_0_OR_GREATER
        [Fact]
        public async Task TestSystemDiagnosticsMetrics()
        {
            var counterNamesP1 = new[] { "test-counter", "test-gauge" };
            var counterNamesP2 = new[] { "test-counter" };

            MetricProvider p1 = new MetricProvider()
            {
                ProviderName = "P1"
            };

            MetricProvider p2 = new MetricProvider()
            {
                ProviderName = "P2"
            };

            var providers = new List<MetricProvider>()
            {
                p1, p2
            };

            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Connect,
                TestAppScenarios.Metrics.Name,
                appValidate: async (runner, client) =>
                {
                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);

                    using ResponseStreamHolder holder = await client.CaptureMetricsAsync(await runner.ProcessIdTask,
                        durationSeconds: 2,
                        metricsConfiguration: new EventMetricsConfiguration
                        {
                            IncludeDefaultProviders = false,
                            Providers = new[]
                            {
                                new EventMetricsProvider
                                {
                                    ProviderName = p1.ProviderName,
                                    CounterNames = counterNamesP1,
                                },
                                new EventMetricsProvider
                                {
                                    ProviderName = p2.ProviderName,
                                    CounterNames = counterNamesP2,
                                }
                            }
                        });

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);

                    var metrics = LiveMetricsTestUtilities.GetAllMetrics(holder.Stream);

                    await LiveMetricsTestUtilities.ValidateMetrics(new[] { p1.ProviderName, p2.ProviderName },
                        counterNamesP1,
                        metrics,
                        strict: true);
                },
                configureTool: runner =>
                {
                    runner.WriteKeyPerValueConfiguration(new RootOptions()
                    {
                        Metrics = new MetricsOptions()
                        {
                            Enabled = true,
                            IncludeDefaultProviders = false,
                            Providers = providers
                        },
                        GlobalCounter = new GlobalCounterOptions()
                        {
                            IntervalSeconds = 1
                        }
                    });
                });
        }
#endif
    }
}
