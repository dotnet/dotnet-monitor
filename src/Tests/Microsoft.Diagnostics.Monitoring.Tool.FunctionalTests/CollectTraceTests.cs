// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class CollectTraceTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public CollectTraceTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

#if NET5_0_OR_GREATER
        [Fact]
        public Task StopOnEvent_Succeeds_WithMatchingOpcode()
        {
            return StopOnEventTestCore(TraceEventOpcode.Reply);
        }

        [Fact]
        public Task StopOnEvent_DoesNotStop_WhenOpcodeDoesNotMatch()
        {
            return Assert.ThrowsAsync<TaskCanceledException>(() => StopOnEventTestCore(TraceEventOpcode.Resume));
        }

        [Fact]
        public Task StopOnEvent_UsesDuration_WhenNoEventMatchesInTime()
        {
            return StopOnEventTestCore(TraceEventOpcode.Resume, TimeSpan.FromSeconds(10));
        }

        private async Task StopOnEventTestCore(TraceEventOpcode opcode = TraceEventOpcode.Info, TimeSpan? duration = null)
        {
            const string DefaultRuleName = "FunctionalTestRule";
            const string EgressProvider = "TmpEgressProvider";
            const string EventProviderName = "TestScenario";
            const string StoppingEventName = "UniqueEvent";

            string qualifiedEventName = (opcode == TraceEventOpcode.Info) ? StoppingEventName : $"{StoppingEventName}/{opcode}";

            using TemporaryDirectory tempDirectory = new(_outputHelper);

            Task ruleCompletedTask = null;

            await ScenarioRunner.SingleTarget(_outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                TestAppScenarios.TraceEvents.Name,
                appValidate: async (appRunner, apiClient) =>
                {
                    await appRunner.SendCommandAsync(TestAppScenarios.TraceEvents.Commands.EmitUniqueEvent);
                    await ruleCompletedTask;
                    await appRunner.SendCommandAsync(TestAppScenarios.TraceEvents.Commands.ShutdownScenario);
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.AddFileSystemEgress(EgressProvider, tempDirectory.FullName);
                    runner.ConfigurationFromEnvironment.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCollectTraceAction(
                            new EventPipeProvider[] {
                                new EventPipeProvider()
                                {
                                    Name = EventProviderName,
                                    Keywords = "-1"
                                }
                            },
                            EgressProvider, options =>
                            {
                                options.Duration = duration ?? TimeSpan.Parse(ActionOptionsConstants.Duration_MaxValue);
                                options.StoppingEvent = new TraceEventOptions()
                                {
                                    ProviderName = EventProviderName,
                                    EventName = qualifiedEventName,
                                };
                            });

                    ruleCompletedTask = runner.WaitForCollectionRuleCompleteAsync(DefaultRuleName);
                });

            string[] files = Directory.GetFiles(tempDirectory.FullName, "*.nettrace", SearchOption.TopDirectoryOnly);
            string traceFile = Assert.Single(files);
            await ValidateNettraceFile(traceFile);
        }

        private async Task ValidateNettraceFile(string filePath)
        {
            byte[] expectedMagicToken = Encoding.UTF8.GetBytes("Nettrace");
            byte[] actualMagicToken = new byte[8];

            await using FileStream fs = File.OpenRead(filePath);
            await fs.ReadAsync(actualMagicToken);
            Assert.True(actualMagicToken.SequenceEqual(expectedMagicToken), $"{filePath} is not a Nettrace file!");
        }
#endif // NET5_0_OR_GREATER
    }
}
