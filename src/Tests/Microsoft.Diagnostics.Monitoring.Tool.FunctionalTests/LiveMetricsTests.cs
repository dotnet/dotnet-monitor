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

        [Fact]
        public async Task TestSystemDiagnosticsMetrics()
        {
            var instrumentNamesP1 = new[] { Constants.CounterName, Constants.GaugeName, Constants.HistogramName1, Constants.HistogramName2 };
            var instrumentNamesP2 = new[] { Constants.CounterName };

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
                            Meters = new[]
                            {
                                new EventMetricsMeter
                                {
                                    MeterName = p1.ProviderName,
                                    InstrumentNames = instrumentNamesP1,
                                },
                                new EventMetricsMeter
                                {
                                    MeterName = p2.ProviderName,
                                    InstrumentNames = instrumentNamesP2,
                                }
                            }
                        });

                    await runner.SendCommandAsync(TestAppScenarios.Metrics.Commands.Continue);

                    var metrics = LiveMetricsTestUtilities.GetAllMetrics(holder.Stream);

                    List<string> actualMeterNames = new();
                    List<string> actualInstrumentNames = new();
                    List<string> actualMetadata = new();

                    await LiveMetricsTestUtilities.AggregateMetrics(metrics, actualMeterNames, actualInstrumentNames, actualMetadata);

                    LiveMetricsTestUtilities.ValidateMetrics(new[] { p1.ProviderName, p2.ProviderName },
                        instrumentNamesP1,
                        actualMeterNames.ToHashSet(),
                        actualInstrumentNames.ToHashSet(),
                        strict: true);

                    // NOTE: This assumes the default percentiles of 50/95/99 - if this changes, this test
                    // will fail and will need to be updated.
                    Regex regex = new Regex(@"\bPercentile=(50|95|99)");

                    for (int index = 0; index < actualMeterNames.Count; ++index)
                    {
                        if (actualInstrumentNames[index] == Constants.HistogramName1)
                        {
                            Assert.Matches(regex, actualMetadata[index]);
                        }
                        else if (actualInstrumentNames[index] == Constants.HistogramName2)
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

        [Fact]
        public async Task TestSystemDiagnosticsMetrics_MaxHistograms()
        {
            var instrumentNames = new[] { Constants.HistogramName1, Constants.HistogramName2 };

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
                            Meters = new[]
                            {
                                new EventMetricsMeter
                                {
                                    MeterName = p1.ProviderName,
                                    InstrumentNames = instrumentNames
                                }
                            }
                        });

                    await runner.SendCommandAsync(TestAppScenarios.Metrics.Commands.Continue);

                    var metrics = LiveMetricsTestUtilities.GetAllMetrics(holder.Stream);

                    List<string> actualMeterNames = new();
                    List<string> actualInstrumentNames = new();
                    List<string> actualMetadata = new();

                    await LiveMetricsTestUtilities.AggregateMetrics(metrics, actualMeterNames, actualInstrumentNames, actualMetadata);

                    Assert.Contains(Constants.HistogramName1, actualInstrumentNames);
                    Assert.DoesNotContain(Constants.HistogramName2, actualInstrumentNames);
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
            var instrumentNames = new[] { Constants.CounterName, Constants.GaugeName, Constants.HistogramName1, Constants.HistogramName2 };

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
                            Meters = new[]
                            {
                                new EventMetricsMeter
                                {
                                    MeterName = p1.ProviderName,
                                    InstrumentNames = instrumentNames
                                }
                            }
                        });

                    await runner.SendCommandAsync(TestAppScenarios.Metrics.Commands.Continue);

                    var metrics = LiveMetricsTestUtilities.GetAllMetrics(holder.Stream);

                    List<string> actualMeterNames = new();
                    List<string> actualInstrumentNames = new();
                    List<string> actualMetadata = new();

                    await LiveMetricsTestUtilities.AggregateMetrics(metrics, actualMeterNames, actualInstrumentNames, actualMetadata);

                    ISet<string> actualNamesSet = new HashSet<string>(actualInstrumentNames);

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

#if NET8_0_OR_GREATER
        [Fact]
        public async Task TestSystemDiagnosticsMetrics_MeterInstrumentTags()
        {
            var instrumentNamesP3 = new[] { Constants.CounterName };

            MetricProvider p3 = new MetricProvider()
            {
                ProviderName = Constants.ProviderName3
            };

            var providers = new List<MetricProvider>()
            {
                p3
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
                            Meters = new[]
                            {
                                new EventMetricsMeter
                                {
                                    MeterName = p3.ProviderName,
                                    InstrumentNames = instrumentNamesP3,
                                }
                            }
                        });

                    await runner.SendCommandAsync(TestAppScenarios.Metrics.Commands.Continue);

                    var metrics = LiveMetricsTestUtilities.GetAllMetrics(holder.Stream);

                    List<string> actualMeterNames = new();
                    List<string> actualInstrumentNames = new();
                    List<string> actualMetadata = new();
                    List<string> actualMeterTags = new();
                    List<string> actualInstrumentTags = new();

                    await LiveMetricsTestUtilities.AggregateMetrics(metrics, actualMeterNames, actualInstrumentNames, actualMetadata, actualMeterTags, actualInstrumentTags);

                    LiveMetricsTestUtilities.ValidateMetrics(new[] { p3.ProviderName },
                        instrumentNamesP3,
                        actualMeterNames.ToHashSet(),
                        actualInstrumentNames.ToHashSet(),
                        strict: true);

                    string actualMeterTag = Assert.Single(actualMeterTags.Distinct());
                    string expectedMeterTag = Constants.MeterMetadataKey + "=" + Constants.MeterMetadataValue;
                    Assert.Equal(expectedMeterTag, actualMeterTag);

                    string actualInstrumentTag = Assert.Single(actualInstrumentTags.Distinct());
                    string expectedInstrumentTag = Constants.InstrumentMetadataKey + "=" + Constants.InstrumentMetadataValue;
                    Assert.Equal(expectedInstrumentTag, actualInstrumentTag);
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
