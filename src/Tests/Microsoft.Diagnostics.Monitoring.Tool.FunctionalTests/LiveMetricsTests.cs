// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Models;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
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
                    
                    var metrics = GetAllMetrics(holder);
                    await ValidateMetrics(new []{ EventPipe.MonitoringSourceConfiguration.SystemRuntimeEventSourceName },
                        new []
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

                    var metrics = GetAllMetrics(holder);
                    await ValidateMetrics(new []{ EventPipe.MonitoringSourceConfiguration.SystemRuntimeEventSourceName },
                        counterNames,
                        metrics,
                        strict: true);

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                });
        }

        private static async Task ValidateMetrics(IEnumerable<string> expectedProviders, IEnumerable<string> expectedNames,
            IAsyncEnumerable<CounterPayload> actualMetrics, bool strict)
        {
            HashSet<string> actualProviders = new();
            HashSet<string> actualNames = new();

            await AggregateMetrics(actualMetrics, actualProviders, actualNames);

            CompareSets(new HashSet<string>(expectedProviders), actualProviders, strict);
            CompareSets(new HashSet<string>(expectedNames), actualNames, strict);
        }

        private static void CompareSets(HashSet<string> expected, HashSet<string> actual, bool strict)
        {
            bool matched = true;
            if (strict && !expected.SetEquals(actual))
            {
                expected.SymmetricExceptWith(actual);
                matched = false;
            }
            else if (!strict && !expected.IsSubsetOf(actual))
            {
                //actual must contain at least the elements in expected, but can contain more
                expected.ExceptWith(actual);
                matched = false;
            }
            Assert.True(matched, "Missing or unexpected elements: " + string.Join(",", expected));
        }

        private static async Task AggregateMetrics(IAsyncEnumerable<CounterPayload> actualMetrics,
            HashSet<string> providers,
            HashSet<string> names)
        {
            await foreach (CounterPayload counter in actualMetrics)
            {
                providers.Add(counter.Provider);
                names.Add(counter.Name);
            }
        }

        private static async IAsyncEnumerable<CounterPayload> GetAllMetrics(ResponseStreamHolder holder)
        {
            using var reader = new StreamReader(holder.Stream);

            string entry = string.Empty;
            while ((entry = await reader.ReadLineAsync()) != null)
            {
                Assert.Equal(StreamingLogger.JsonSequenceRecordSeparator, (byte)entry[0]);
                yield return JsonSerializer.Deserialize<CounterPayload>(entry.Substring(1));
            }
        }
    }
}