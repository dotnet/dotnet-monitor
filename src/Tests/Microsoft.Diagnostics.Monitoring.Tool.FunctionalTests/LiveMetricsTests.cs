// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
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

                    var metrics = LiveMetricsTestUtilities.GetAllSystemDiagnosticsMetrics(holder.Stream);
                    await LiveMetricsTestUtilities.ValidateMetrics(new[] { MonitoringSourceConfiguration.SystemRuntimeEventSourceName },
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
                                    ProviderName = MonitoringSourceConfiguration.SystemRuntimeEventSourceName,
                                    CounterNames = counterNames,
                                }
                            }
                        });

                    var metrics = LiveMetricsTestUtilities.GetAllSystemDiagnosticsMetrics(holder.Stream);
                    await LiveMetricsTestUtilities.ValidateMetrics(new[] { MonitoringSourceConfiguration.SystemRuntimeEventSourceName },
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
            var counterNamesP1 = new[] { "test-counter", "test-gauge", "test-histogram", "test-histogram-2" };
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
                    //await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);

                    using ResponseStreamHolder holder = await client.CaptureMetricsAsync(await runner.ProcessIdTask,
                        durationSeconds: 8,
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

                    var metrics = LiveMetricsTestUtilities.GetAllSystemDiagnosticsMetrics(holder.Stream);

                    List<string> actualProviders = new();
                    List<string> actualNames = new();
                    List<string> actualMetadata = new();

                    await LiveMetricsTestUtilities.AggregateMetrics(metrics, actualProviders, actualNames, actualMetadata);

                    LiveMetricsTestUtilities.ValidateMetrics(new[] { p1.ProviderName, p2.ProviderName },
                        counterNamesP1,
                        actualProviders.ToHashSet(),
                        actualNames.ToHashSet(),
                        strict: true);

                    Regex regex = new Regex(@"\b[Percentile=]\w+?\d{0,2}");

                    for (int index = 0; index < actualProviders.Count; ++index)
                    {
                        if (actualNames[index] == "test-histogram")
                        {
                            Assert.Matches(regex, actualMetadata[index]);
                        }
                        else if (actualNames[index] == "test-histogram-2")
                        {
                            var metadata = actualMetadata[index].Split(',');
                            Assert.Equal(2, metadata.Length);
                            Assert.Equal("key1=value1", metadata[0]);
                            Assert.Matches(regex, metadata[1]);
                        }
                    }
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
