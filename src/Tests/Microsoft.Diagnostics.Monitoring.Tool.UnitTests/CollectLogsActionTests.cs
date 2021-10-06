// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
//using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.UnitTests;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public class CollectLogsActionTests
    {
        private readonly ITestOutputHelper _outputHelper;

        const char JsonSequenceRecordSeparator = '\u001E';

        private const string ExpectedEgressProvider = "TmpEgressProvider";
        private const string DefaultRuleName = "LogsTestRule";

        public CollectLogsActionTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Test that log events with a LogsTestUtilities.Category that doesn't have a specified level are collected
        /// at the log level specified in the request body.
        /// </summary>
        [ConditionalTheory(nameof(SkipOnWindowsNetCore31))]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.NDJson)]
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.NDJson)]
#endif
        public Task LogsDefaultLevelFallbackActionTest(DiagnosticPortConnectionMode mode, LogFormat logFormat)
        {
            return ValidateLogsActionAsync(
                mode,
                new LogsConfiguration()
                {
                    FilterSpecs = new Dictionary<string, LogLevel?>()
                    {
                        { TestAppScenarios.Logger.Categories.LoggerCategory1, LogLevel.Error },
                        { TestAppScenarios.Logger.Categories.LoggerCategory2, null },
                        { TestAppScenarios.Logger.Categories.LoggerCategory3, LogLevel.Warning }
                    },
                    LogLevel = LogLevel.Information,
                    UseAppFilters = false
                },
                async reader =>
                {
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1CriticalEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2InformationEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2CriticalEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3CriticalEntry, await reader.ReadAsync());
                    Assert.False(await reader.WaitToReadAsync());
                },
                logFormat);
        }

/*
        /// <summary>
        /// Test that LogLevel.None is not supported as the default log level in the request body.
        /// </summary>
        [ConditionalTheory(nameof(SkipOnWindowsNetCore31))]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.NDJson)]
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.NDJson)]
#endif
        public Task LogsDefaultLevelNoneNotSupportedViaBodyActionTest(DiagnosticPortConnectionMode mode, LogFormat logFormat)
        {
            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                mode,
                TestAppScenarios.Logger.Name,
                appValidate: async (runner, client) =>
                {
                    ValidationProblemDetailsException exception = await Assert.ThrowsAsync<ValidationProblemDetailsException>(
                        async () =>
                        {
                            using ResponseStreamHolder _ = await client.CaptureLogsAsync(
                                await runner.ProcessIdTask,
                                TestTimeouts.LogsDuration,
                                new LogsConfiguration() { LogLevel = LogLevel.None },
                                logFormat);
                        });
                    Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
                    Assert.Equal(StatusCodes.Status400BadRequest, exception.Details.Status);

                    // Allow test app to gracefully exit by continuing the scenario.
                    await runner.SendCommandAsync(TestAppScenarios.Logger.Commands.StartLogging);
                });
        }
*/

        /// <summary>
        /// Test that log events are collected for the categories and levels specified by the application.
        /// </summary>
        [ConditionalTheory(nameof(SkipOnWindowsNetCore31))]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.NDJson)]
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.NDJson)]
#endif
        public Task LogsUseAppFiltersViaBodyActionTest(DiagnosticPortConnectionMode mode, LogFormat logFormat)
        {
            return ValidateLogsActionAsync(
                mode,
                new LogsConfiguration()
                {
                    LogLevel = LogLevel.Trace,
                    UseAppFilters = true
                },
                async reader =>
                {
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1DebugEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1InformationEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1CriticalEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2InformationEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2CriticalEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3CriticalEntry, await reader.ReadAsync());
                    Assert.False(await reader.WaitToReadAsync());
                },
                logFormat);
        }

        /// <summary>
        /// Test that log events are collected for the categories and levels specified by the application
        /// and for the categories and levels specified in the filter specs.
        /// </summary>
        [ConditionalTheory(nameof(SkipOnWindowsNetCore31))]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.NDJson)]
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.NDJson)]
#endif
        public Task LogsUseAppFiltersAndFilterSpecsActionTest(DiagnosticPortConnectionMode mode, LogFormat logFormat)
        {
            return ValidateLogsActionAsync(
                mode,
                new LogsConfiguration()
                {
                    FilterSpecs = new Dictionary<string, LogLevel?>()
                    {
                        { TestAppScenarios.Logger.Categories.LoggerCategory3, LogLevel.Debug }
                    },
                    LogLevel = LogLevel.Trace,
                    UseAppFilters = true
                },
                async reader =>
                {
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1DebugEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1InformationEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1CriticalEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2InformationEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2CriticalEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3DebugEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3InformationEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3CriticalEntry, await reader.ReadAsync());
                    Assert.False(await reader.WaitToReadAsync());
                },
                logFormat);
        }

        /// <summary>
        /// Test that log events are collected for wildcard categories.
        /// </summary>
        [ConditionalTheory(nameof(SkipOnWindowsNetCore31))]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.NDJson)]
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.NDJson)]
#endif
        public Task LogsWildcardActionTest(DiagnosticPortConnectionMode mode, LogFormat logFormat)
        {
            return ValidateLogsActionAsync(
                mode,
                new LogsConfiguration()
                {
                    FilterSpecs = new Dictionary<string, LogLevel?>()
                    {
                        { "*", LogLevel.Trace },
                        { TestAppScenarios.Logger.Categories.LoggerCategory2, LogLevel.Warning }
                    },
                    LogLevel = LogLevel.Information,
                    UseAppFilters = false
                },
                async reader =>
                {
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1TraceEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1DebugEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1InformationEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1CriticalEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2CriticalEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3TraceEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3DebugEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3InformationEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3CriticalEntry, await reader.ReadAsync());
                    Assert.False(await reader.WaitToReadAsync());
                },
                logFormat);
        }

        private async Task ValidateLogsActionAsync(
            DiagnosticPortConnectionMode mode,
            LogsConfiguration configuration,
            Func<ChannelReader<LogEntry>, Task> callback,
            LogFormat logFormat)
        {
            EndpointUtilities _endpointUtilities = new(_outputHelper);

            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddFileSystemEgress(ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectLogsAction(ExpectedEgressProvider, out CollectLogsOptions collectLogsOptions)
                    .SetStartupTrigger();

                collectLogsOptions.Duration = CommonTestTimeouts.LogsDuration;
                collectLogsOptions.FilterSpecs = configuration.FilterSpecs;
                collectLogsOptions.DefaultLevel = configuration.LogLevel;
                collectLogsOptions.Format = logFormat;
                collectLogsOptions.UseAppFilters = configuration.UseAppFilters;
            }, async host =>
            {
                IOptionsMonitor<CollectionRuleOptions> ruleOptionsMonitor = host.Services.GetService<IOptionsMonitor<CollectionRuleOptions>>();
                CollectLogsOptions options = (CollectLogsOptions)ruleOptionsMonitor.Get(DefaultRuleName).Actions[0].Settings;

                // This is reassigned here due to a quirk in which a null value in the dictionary has its key removed, thus causing LogsDefaultLevelFallbackActionTest to fail. By reassigning here, any keys with null values are maintained.
                options.FilterSpecs = configuration.FilterSpecs;

                ICollectionRuleActionFactoryProxy factory;
                Assert.True(host.Services.GetService<ICollectionRuleActionOperations>().TryCreateFactory(KnownCollectionRuleActions.CollectLogs, out factory));

                using CancellationTokenSource endpointTokenSource = new CancellationTokenSource(CommonTestTimeouts.LogsTimeout);

                await SingleTarget(
                    _outputHelper,
                    mode,
                    TestAppScenarios.Logger.Name,
                    appValidate: async (runner) =>
                    {
                        IEndpointInfo endpointInfo = await EndpointInfo.FromProcessIdAsync(await runner.ProcessIdTask, endpointTokenSource.Token);
                        ICollectionRuleAction action = factory.Create(endpointInfo, options);

                        using CancellationTokenSource validationTokenSource = new CancellationTokenSource(CommonTestTimeouts.LogsTimeout);

                        await ValidateResponseStream(
                            runner,
                            action,
                            callback,
                            logFormat);
                    });
            });
        }

        private async Task ValidateResponseStream(AppRunner runner, ICollectionRuleAction action, Func<ChannelReader<LogEntry>, Task> callback, LogFormat logFormat)
        {
            Assert.NotNull(runner);
            Assert.NotNull(action);
            Assert.NotNull(callback);

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            await action.StartAsync(cancellationTokenSource.Token);

            // CONSIDER: Give dotnet-monitor some time to start the logs pipeline before having the target
            // application start logging. It would be best if dotnet-monitor could write a console event
            // (at Debug or Trace level) for when the pipeline has started. This would require dotnet-monitor
            // to know when the pipeline started and is waiting for logging data.
            await Task.Delay(TimeSpan.FromSeconds(3));

            // Start logging in the target application
            await runner.SendCommandAsync(TestAppScenarios.Logger.Commands.StartLogging);

            CollectionRuleActionResult result = await action.WaitForCompletionAsync(cancellationTokenSource.Token);

            Assert.NotNull(result.OutputValues);
            Assert.True(result.OutputValues.TryGetValue(CollectionRuleActionConstants.EgressPathOutputValueName, out string egressPath));
            Assert.True(File.Exists(egressPath));

            using FileStream logsStream = new(egressPath, FileMode.Open, FileAccess.Read);
            Assert.NotNull(logsStream);

            await ValidateLogsEquality(logsStream, callback, logFormat);
        }

        private async Task ValidateLogsEquality(Stream logsStream, Func<ChannelReader<LogEntry>, Task> callback, LogFormat logFormat)
        {
            // Set up a channel and process the log events here rather than having each test have to deserialize
            // the set of log events. Pass the channel reader to the callback to allow each test to verify the
            // set of deserialized log events.
            Channel<LogEntry> channel = Channel.CreateUnbounded<LogEntry>(new UnboundedChannelOptions()
            {
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = false
            });

            Task callbackTask = callback(channel.Reader);

            using StreamReader reader = new StreamReader(logsStream);

            JsonSerializerOptions options = new();
            options.Converters.Add(new JsonStringEnumConverter());

            _outputHelper.WriteLine("Begin reading log entries.");
            string line;

            while (null != (line = await reader.ReadLineAsync()))
            {
                if (logFormat == LogFormat.JsonSequence)
                {
                    Assert.True(line.Length > 1);
                    Assert.Equal(JsonSequenceRecordSeparator, line[0]);
                    Assert.NotEqual(JsonSequenceRecordSeparator, line[1]);

                    line = line.TrimStart(JsonSequenceRecordSeparator);
                }

                _outputHelper.WriteLine("Log entry: {0}", line);
                try
                {
                    await channel.Writer.WriteAsync(JsonSerializer.Deserialize<LogEntry>(line, options));
                }
                catch (JsonException ex)
                {
                    _outputHelper.WriteLine("Exception while deserializing log entry: {0}", ex);
                }
            }
            _outputHelper.WriteLine("End reading log entries.");
            channel.Writer.Complete();

            await callbackTask;
        }

        private static async Task SingleTarget(
            ITestOutputHelper outputHelper,
            DiagnosticPortConnectionMode mode,
            string scenarioName,
            Func<AppRunner, Task> appValidate,
            Action<AppRunner> configureApp = null)
        {
            DiagnosticPortHelper.Generate(
                mode,
                out DiagnosticPortConnectionMode appConnectionMode,
                out string diagnosticPortPath);

            /*
            await using MonitorCollectRunner toolRunner = new(outputHelper);
            toolRunner.ConnectionMode = mode;
            toolRunner.DiagnosticPortPath = diagnosticPortPath;
            toolRunner.DisableAuthentication = true;
            toolRunner.DisableHttpEgress = false;

            //configureTool?.Invoke(toolRunner);

            await toolRunner.StartAsync();
            */
            AppRunner appRunner = new(outputHelper, Assembly.GetExecutingAssembly());
            appRunner.ConnectionMode = appConnectionMode;
            appRunner.DiagnosticPortPath = diagnosticPortPath;
            appRunner.ScenarioName = scenarioName;

            configureApp?.Invoke(appRunner);

            await appRunner.ExecuteAsync(async () =>
            {
                await appValidate(appRunner);
            });
            Assert.Equal(0, appRunner.ExitCode);
        }

        public static bool SkipOnWindowsNetCore31
        {
            get
            {
                // Skip logs tests for .NET Core 3.1 on Windows; these tests sporadically
                // fail frequently causing insertions and builds with unrelated changes to
                // fail. See https://github.com/dotnet/dotnet-monitor/issues/807 for details.
                return !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                    DotNetHost.RuntimeVersion.Major != 3 ||
                    DotNetHost.RuntimeVersion.Minor != 1;
            }
        }
    }
}