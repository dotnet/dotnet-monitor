﻿// Licensed to the .NET Foundation under one or more agreements.
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Constants = Microsoft.Diagnostics.Monitoring.TestCommon.LiveMetricsTestConstants;

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
                        durationSeconds: CommonTestTimeouts.LiveMetricsDurationSeconds);

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
                },
                configureTool: runner =>
                {
                    runner.WriteKeyPerValueConfiguration(new RootOptions()
                    {
                        GlobalCounter = new GlobalCounterOptions()
                        {
                            IntervalSeconds = 1
                        }
                    });
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
                        durationSeconds: CommonTestTimeouts.LiveMetricsDurationSeconds,
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
                },
                configureTool: runner =>
                {
                    runner.WriteKeyPerValueConfiguration(new RootOptions()
                    {
                        GlobalCounter = new GlobalCounterOptions()
                        {
                            IntervalSeconds = 1
                        }
                    });
                });
        }

        [Theory]
        [InlineData(MetricProviderType.All, true)]
        [InlineData(MetricProviderType.Meter, false)]
        [InlineData(MetricProviderType.EventCounter, true)]
        public Task TestCustomMetrics_MetricProviderType(MetricProviderType metricType, bool expectResults)
        {
            return ScenarioRunner.SingleTarget(_outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Connect,
                TestAppScenarios.AsyncWait.Name,
                async (appRunner, apiClient) =>
                {
                    var counterNames = new[] { "cpu-usage", "working-set" };

                    using ResponseStreamHolder holder = await apiClient.CaptureMetricsAsync(await appRunner.ProcessIdTask,
                        durationSeconds: CommonTestTimeouts.LiveMetricsDurationSeconds,
                        metricsConfiguration: new EventMetricsConfiguration
                        {
                            IncludeDefaultProviders = false,
                            Providers = new[]
                            {
                                new EventMetricsProvider
                                {
                                    ProviderName = EventPipe.MonitoringSourceConfiguration.SystemRuntimeEventSourceName,
                                    CounterNames = counterNames,
                                    MetricType = metricType
                                }
                            }
                        });

                    var metrics = LiveMetricsTestUtilities.GetAllMetrics(holder.Stream);

                    List<string> actualProviders = new();
                    List<string> actualNames = new();
                    List<string> actualMetadata = new();

                    await LiveMetricsTestUtilities.AggregateMetrics(metrics, actualProviders, actualNames, actualMetadata);

                    if (expectResults)
                    {
                        LiveMetricsTestUtilities.ValidateMetrics(new[] { EventPipe.MonitoringSourceConfiguration.SystemRuntimeEventSourceName },
                            counterNames,
                            actualProviders.ToHashSet(),
                            actualNames.ToHashSet(),
                            strict: true);
                    }
                    else
                    {
                        Assert.Empty(actualProviders);
                        Assert.Empty(actualNames);
                    }

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: runner =>
                {
                    runner.WriteKeyPerValueConfiguration(new RootOptions()
                    {
                        GlobalCounter = new GlobalCounterOptions()
                        {
                            IntervalSeconds = 1
                        }
                    });
                });
        }

        [Fact]
        public async Task TestSystemDiagnosticsMetrics()
        {
            var counterNamesP1 = new[] { Constants.CounterName, Constants.GaugeName, Constants.HistogramName1, Constants.HistogramName2 };
            var counterNamesP2 = new[] { Constants.CounterName };

            MetricProvider p1 = new MetricProvider()
            {
                ProviderName = Constants.ProviderName1
            };

            MetricProvider p2 = new MetricProvider()
            {
                ProviderName = Constants.ProviderName2
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
                    using ResponseStreamHolder holder = await client.CaptureMetricsAsync(await runner.ProcessIdTask,
                        durationSeconds: CommonTestTimeouts.LiveMetricsDurationSeconds,
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

                    await runner.SendCommandAsync(TestAppScenarios.Metrics.Commands.Continue);

                    var metrics = LiveMetricsTestUtilities.GetAllMetrics(holder.Stream);

                    List<string> actualProviders = new();
                    List<string> actualNames = new();
                    List<string> actualMetadata = new();

                    await LiveMetricsTestUtilities.AggregateMetrics(metrics, actualProviders, actualNames, actualMetadata);

                    LiveMetricsTestUtilities.ValidateMetrics(new[] { p1.ProviderName, p2.ProviderName },
                        counterNamesP1,
                        actualProviders.ToHashSet(),
                        actualNames.ToHashSet(),
                        strict: true);

                    // NOTE: This assumes the default percentiles of 50/95/99 - if this changes, this test
                    // will fail and will need to be updated.
                    Regex regex = new Regex(@"\bPercentile=(50|95|99)");

                    for (int index = 0; index < actualProviders.Count; ++index)
                    {
                        if (actualNames[index] == Constants.HistogramName1)
                        {
                            Assert.Matches(regex, actualMetadata[index]);
                        }
                        else if (actualNames[index] == Constants.HistogramName2)
                        {
                            var metadata = actualMetadata[index].Split(',');
                            Assert.Equal(2, metadata.Length);
                            Assert.Equal(FormattableString.Invariant($"{Constants.MetadataKey}={Constants.MetadataValue}"), metadata[0]);
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

        [Theory]
        [InlineData(MetricProviderType.All, true)]
        [InlineData(MetricProviderType.Meter, true)]
        [InlineData(MetricProviderType.EventCounter, false)]
        public async Task TestSystemDiagnosticsMetrics_MetricProviderType(MetricProviderType metricType, bool expectResults)
        {
            var counterNames = new[] { Constants.CounterName };

            MetricProvider p1 = new MetricProvider()
            {
                ProviderName = Constants.ProviderName1
            };

            var providers = new List<MetricProvider>()
            {
                p1
            };

            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Connect,
                TestAppScenarios.Metrics.Name,
                appValidate: async (runner, client) =>
                {
                    using ResponseStreamHolder holder = await client.CaptureMetricsAsync(await runner.ProcessIdTask,
                        durationSeconds: CommonTestTimeouts.LiveMetricsDurationSeconds,
                        metricsConfiguration: new EventMetricsConfiguration
                        {
                            IncludeDefaultProviders = false,
                            Providers = new[]
                            {
                                new EventMetricsProvider
                                {
                                    ProviderName = p1.ProviderName,
                                    CounterNames = counterNames,
                                    MetricType = metricType
                                }
                            }
                        });

                    await runner.SendCommandAsync(TestAppScenarios.Metrics.Commands.Continue);

                    var metrics = LiveMetricsTestUtilities.GetAllMetrics(holder.Stream);

                    List<string> actualProviders = new();
                    List<string> actualNames = new();
                    List<string> actualMetadata = new();

                    await LiveMetricsTestUtilities.AggregateMetrics(metrics, actualProviders, actualNames, actualMetadata);

                    if (expectResults)
                    {
                        LiveMetricsTestUtilities.ValidateMetrics(new[] { p1.ProviderName },
                            counterNames,
                            actualProviders.ToHashSet(),
                            actualNames.ToHashSet(),
                            strict: true);
                    }
                    else
                    {
                        Assert.Empty(actualProviders);
                        Assert.Empty(actualNames);
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

        [Fact]
        public async Task TestSystemDiagnosticsMetrics_MaxHistograms()
        {
            var counterNames = new[] { Constants.HistogramName1, Constants.HistogramName2 };

            MetricProvider p1 = new MetricProvider()
            {
                ProviderName = Constants.ProviderName1
            };

            var providers = new List<MetricProvider>()
            {
                p1
            };

            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Connect,
                TestAppScenarios.Metrics.Name,
                appValidate: async (runner, client) =>
                {
                    using ResponseStreamHolder holder = await client.CaptureMetricsAsync(await runner.ProcessIdTask,
                        durationSeconds: CommonTestTimeouts.LiveMetricsDurationSeconds,
                        metricsConfiguration: new EventMetricsConfiguration
                        {
                            IncludeDefaultProviders = false,
                            Providers = new[]
                            {
                                new EventMetricsProvider
                                {
                                    ProviderName = p1.ProviderName,
                                    CounterNames = counterNames
                                }
                            }
                        });

                    await runner.SendCommandAsync(TestAppScenarios.Metrics.Commands.Continue);

                    var metrics = LiveMetricsTestUtilities.GetAllMetrics(holder.Stream);

                    List<string> actualProviders = new();
                    List<string> actualNames = new();
                    List<string> actualMetadata = new();

                    await LiveMetricsTestUtilities.AggregateMetrics(metrics, actualProviders, actualNames, actualMetadata);

                    Assert.Contains(Constants.HistogramName1, actualNames);
                    Assert.DoesNotContain(Constants.HistogramName2, actualNames);
                },
                configureTool: runner =>
                {
                    runner.WriteKeyPerValueConfiguration(new RootOptions()
                    {
                        Metrics = new MetricsOptions()
                        {
                            Enabled = true,
                            IncludeDefaultProviders = false,
                            Providers = providers,
                        },
                        GlobalCounter = new GlobalCounterOptions()
                        {
                            IntervalSeconds = 1,
                            MaxHistograms = 1
                        }
                    });
                });
        }

        [Fact]
        public async Task TestSystemDiagnosticsMetrics_MaxTimeseries()
        {
            var counterNames = new[] { Constants.CounterName, Constants.GaugeName, Constants.HistogramName1, Constants.HistogramName2 };

            const int maxTimeSeries = 3;

            MetricProvider p1 = new MetricProvider()
            {
                ProviderName = Constants.ProviderName1
            };

            var providers = new List<MetricProvider>()
            {
                p1
            };

            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Connect,
                TestAppScenarios.Metrics.Name,
                appValidate: async (runner, client) =>
                {
                    using ResponseStreamHolder holder = await client.CaptureMetricsAsync(await runner.ProcessIdTask,
                        durationSeconds: CommonTestTimeouts.LiveMetricsDurationSeconds,
                        metricsConfiguration: new EventMetricsConfiguration
                        {
                            IncludeDefaultProviders = false,
                            Providers = new[]
                            {
                                new EventMetricsProvider
                                {
                                    ProviderName = p1.ProviderName,
                                    CounterNames = counterNames
                                }
                            }
                        });

                    await runner.SendCommandAsync(TestAppScenarios.Metrics.Commands.Continue);

                    var metrics = LiveMetricsTestUtilities.GetAllMetrics(holder.Stream);

                    List<string> actualProviders = new();
                    List<string> actualNames = new();
                    List<string> actualMetadata = new();

                    await LiveMetricsTestUtilities.AggregateMetrics(metrics, actualProviders, actualNames, actualMetadata);

                    ISet<string> actualNamesSet = new HashSet<string>(actualNames);

                    Assert.Equal(maxTimeSeries, actualNamesSet.Count);
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
                            IntervalSeconds = 1,
                            MaxTimeSeries = maxTimeSeries
                        }
                    });
                });
        }
    }
}
