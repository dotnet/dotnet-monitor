// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class CollectTraceTests
    {
        TimeSpan DefaultNotStoppedCollectTraceTimeout = TimeSpan.FromSeconds(15);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public CollectTraceTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

        [Fact]
        public Task StopOnEvent_Succeeds_WithMatchingOpcode()
        {
            return StopOnEventTestCore(expectStoppingEvent: true);
        }

        [Fact]
        public Task StopOnEvent_Succeeds_WithMatchingOpcodeAndNoRundown()
        {
            return StopOnEventTestCore(expectStoppingEvent: true, collectRundown: false);
        }

        [Fact]
        public Task StopOnEvent_Succeeds_WithMatchingPayload()
        {
            return StopOnEventTestCore(expectStoppingEvent: true, payloadFilter: new Dictionary<string, string>()
            {
                { TestAppScenarios.TraceEvents.UniqueEventPayloadField, TestAppScenarios.TraceEvents.UniqueEventMessage }
            });
        }

        [Fact]
        public Task StopOnEvent_DoesNotStop_WhenOpcodeDoesNotMatch()
        {
            return StopOnEventTestCore(expectStoppingEvent: false, opcode: TraceEventOpcode.Resume, duration: DefaultNotStoppedCollectTraceTimeout);
        }

        [Fact]
        public Task StopOnEvent_DoesNotStop_WhenPayloadFieldNamesMismatch()
        {
            return StopOnEventTestCore(expectStoppingEvent: false, payloadFilter: new Dictionary<string, string>()
            {
                { TestAppScenarios.TraceEvents.UniqueEventPayloadField, TestAppScenarios.TraceEvents.UniqueEventMessage },
                { "foobar", "baz" }
            },
            duration: DefaultNotStoppedCollectTraceTimeout);
        }

        [Fact]
        public Task StopOnEvent_DoesNotStop_WhenPayloadFieldValueMismatch()
        {
            return StopOnEventTestCore(expectStoppingEvent: false, payloadFilter: new Dictionary<string, string>()
            {
                { TestAppScenarios.TraceEvents.UniqueEventPayloadField, TestAppScenarios.TraceEvents.UniqueEventMessage.ToUpperInvariant() }
            },
            duration: DefaultNotStoppedCollectTraceTimeout);
        }

        [Fact]
        public async Task ExitOnStdinDisconnect_Succeeds()
        {
            // Start an app in 'connect' mode to match what the Visual Studio profiler does.
            DiagnosticPortHelper.Generate(
                DiagnosticPortConnectionMode.Listen,
                out DiagnosticPortConnectionMode appConnectionMode,
                out string diagnosticPortPath);

            await using AppRunner appRunner = new(_outputHelper, Assembly.GetExecutingAssembly());
            appRunner.ConnectionMode = appConnectionMode;
            appRunner.DiagnosticPortPath = diagnosticPortPath;
            appRunner.ScenarioName = TestAppScenarios.AspNet.Name;
            using (CancellationTokenSource appStartCancellationTokenSource = new(CommonTestTimeouts.StartProcess))
            {
                // Start the app, but don't wait for it to be ready since we're not going to make any requests.
                await appRunner.StartAsync(appStartCancellationTokenSource.Token, waitForReady: false);
            }

            // Start dotnet-monitor in `--exit-on-stdin-disconnect` mode.
            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.ConnectionModeViaCommandLine = DiagnosticPortConnectionMode.Listen;
            toolRunner.DiagnosticPortPath = diagnosticPortPath;
            toolRunner.DisableAuthentication = true;
            toolRunner.ExitOnStdinDisconnect = true;
            await toolRunner.StartAsync();
            Assert.False(toolRunner.HasExited);

            // Close the tool's stdin, which should cause it to exit.
            toolRunner.StandardInput.Close();

            using (CancellationTokenSource monitorExitCancellationTokenSource = new(TestTimeouts.DotnetMonitorExitAfterStdinCloseTimeout))
            {
                await toolRunner.WaitForExitAsync(monitorExitCancellationTokenSource.Token);
            }

            // Verify that everything shutdown cleanly
            Assert.Equal(0, toolRunner.ExitCode);
            Assert.False(appRunner.HasExited);

            await appRunner.StopAsync();
        }

        [Fact]
        public Task StopOnOperation_Succeeds()
        {
            return StopOnEventTestCore(expectStoppingEvent: false,
                collectRundown: true,
                opcode: TraceEventOpcode.Resume,
                stopWithApi: true);
        }

        private static string ConstructQualifiedEventName(string eventName, TraceEventOpcode opcode)
        {
            return (opcode == TraceEventOpcode.Info)
                ? eventName
                : FormattableString.Invariant($"{eventName}/{opcode}");
        }

        private async Task StopOnEventTestCore(bool expectStoppingEvent,
            TraceEventOpcode opcode = TestAppScenarios.TraceEvents.UniqueEventOpcode,
            bool collectRundown = true,
            IDictionary<string, string> payloadFilter = null,
            TimeSpan? duration = null,
            bool stopWithApi = false)
        {
            const string DefaultRuleName = "FunctionalTestRule";
            const string EgressProvider = "TmpEgressProvider";

            using TemporaryDirectory tempDirectory = new(_outputHelper);

            Task ruleCompletedTask = null;

            TraceEventFilter traceEventFilter = new()
            {
                ProviderName = TestAppScenarios.TraceEvents.EventProviderName,
                EventName = ConstructQualifiedEventName(TestAppScenarios.TraceEvents.UniqueEventName, opcode),
                PayloadFilter = payloadFilter
            };

            await ScenarioRunner.SingleTarget(_outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                TestAppScenarios.TraceEvents.Name,
                appValidate: async (appRunner, apiClient) =>
                {
                    await appRunner.SendCommandAsync(TestAppScenarios.TraceEvents.Commands.EmitUniqueEvent);

                    if (stopWithApi)
                    {
                        var operations = await apiClient.GetOperations();
                        Assert.Single(operations);
                        await apiClient.StopEgressOperation(operations.First().OperationId);
                    }

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
                                    Name = TestAppScenarios.TraceEvents.EventProviderName,
                                    Keywords = "-1"
                                }
                            },
                            EgressProvider, options =>
                            {
                                options.Duration = duration ?? CommonTestTimeouts.GeneralTimeout;
                                options.StoppingEvent = traceEventFilter;
                                options.RequestRundown = collectRundown;
                            });

                    ruleCompletedTask = runner.WaitForCollectionRuleCompleteAsync(DefaultRuleName);
                });

            string[] files = Directory.GetFiles(tempDirectory.FullName, "*.nettrace", SearchOption.TopDirectoryOnly);
            string traceFile = Assert.Single(files);

            var (hasStoppingEvent, hasRundown) = await ValidateNettraceFile(traceFile, traceEventFilter);
            Assert.Equal(expectStoppingEvent, hasStoppingEvent);
            Assert.Equal(collectRundown, hasRundown);
        }

        private static Task<(bool hasStoppingEvent, bool hasRundown)> ValidateNettraceFile(string filePath, TraceEventFilter eventFilter)
        {
            return Task.Run(() =>
            {
                using FileStream fs = File.OpenRead(filePath);
                using EventPipeEventSource eventSource = new(fs);

                bool didSeeRundownEvents = false;
                bool didSeeStoppingEvent = false;

                eventSource.Dynamic.AddCallbackForProviderEvent(eventFilter.ProviderName, eventFilter.EventName, (obj) =>
                {
                    if (eventFilter.PayloadFilter != null)
                    {
                        foreach (var (fieldName, fieldValue) in eventFilter.PayloadFilter)
                        {
                            object payloadValue = obj.PayloadByName(fieldName);
                            if (!string.Equals(fieldValue, payloadValue?.ToString(), StringComparison.Ordinal))
                            {
                                return;
                            }
                        }
                    }

                    didSeeStoppingEvent = true;
                });

                ClrRundownTraceEventParser rundown = new(eventSource);
                rundown.RuntimeStart += (data) =>
                {
                    didSeeRundownEvents = true;
                };

                eventSource.Process();
                return (didSeeStoppingEvent, didSeeRundownEvents);
            });
        }
    }
}
