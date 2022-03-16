// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class LogsTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public LogsTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Tests that all log events are collected if log level set to Trace.
        /// </summary>
        [ConditionalTheory(nameof(IsNotWindowsNetCore31))]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.NewlineDelimitedJson)]

#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.NewlineDelimitedJson)]
#endif
        public Task LogsAllCategoriesTest(DiagnosticPortConnectionMode mode, LogFormat logFormat)
        {
            return ValidateLogsAsync(
                mode,
                LogLevel.Trace,
                async reader =>
                {
                    // Default LogLevel.Trace is converted to EventLevel.LogAlways but
                    // runtime does not translate that back to LogLevel.Trace however it
                    // falls back to capturing LogLevel.Debug and above. Thus, no Trace
                    // events will ever be collected if relying on default log level.

                    //LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1TraceEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1DebugEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1InformationEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1CriticalEntry, await reader.ReadAsync());
                    //LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2TraceEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2DebugEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2InformationEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category2CriticalEntry, await reader.ReadAsync());
                    //LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category3TraceEntry, await reader.ReadAsync());
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
        /// Tests that log events with level at or above the specified level are collected.
        /// </summary>
        [ConditionalTheory(nameof(IsNotWindowsNetCore31))]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.NewlineDelimitedJson)]
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.NewlineDelimitedJson)]
#endif
        public Task LogsDefaultLevelTest(DiagnosticPortConnectionMode mode, LogFormat logFormat)
        {
            return ValidateLogsAsync(
                mode,
                LogLevel.Warning,
                async reader =>
                {
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1WarningEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1ErrorEntry, await reader.ReadAsync());
                    LogsTestUtilities.ValidateEntry(LogsTestUtilities.Category1CriticalEntry, await reader.ReadAsync());
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
        /// Test that log events with a category that doesn't have a specified level are collected
        /// at the log level specified in the request body.
        /// </summary>
        [ConditionalTheory(nameof(IsNotWindowsNetCore31))]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.NewlineDelimitedJson)]
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.NewlineDelimitedJson)]
#endif
        public Task LogsDefaultLevelFallbackTest(DiagnosticPortConnectionMode mode, LogFormat logFormat)
        {
            return ValidateLogsAsync(
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

        /// <summary>
        /// Test that LogLevel.None is not supported as the level query parameter.
        /// </summary>
        [ConditionalTheory(nameof(IsNotWindowsNetCore31))]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.NewlineDelimitedJson)]
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.NewlineDelimitedJson)]
#endif
        public Task LogsDefaultLevelNoneNotSupportedViaQueryTest(DiagnosticPortConnectionMode mode, LogFormat logFormat)
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
                                CommonTestTimeouts.LogsDuration,
                                LogLevel.None,
                                logFormat);
                        });
                    Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
                    Assert.Equal(StatusCodes.Status400BadRequest, exception.Details.Status);

                    // Allow test app to gracefully exit by continuing the scenario.
                    await runner.SendCommandAsync(TestAppScenarios.Logger.Commands.StartLogging);
                });
        }

        /// <summary>
        /// Test that LogLevel.None is not supported as the default log level in the request body.
        /// </summary>
        [ConditionalTheory(nameof(IsNotWindowsNetCore31))]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.NewlineDelimitedJson)]
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.NewlineDelimitedJson)]
#endif
        public Task LogsDefaultLevelNoneNotSupportedViaBodyTest(DiagnosticPortConnectionMode mode, LogFormat logFormat)
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
                                CommonTestTimeouts.LogsDuration,
                                new LogsConfiguration() { LogLevel = LogLevel.None },
                                logFormat);
                        });
                    Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
                    Assert.Equal(StatusCodes.Status400BadRequest, exception.Details.Status);

                    // Allow test app to gracefully exit by continuing the scenario.
                    await runner.SendCommandAsync(TestAppScenarios.Logger.Commands.StartLogging);
                });
        }

        /// <summary>
        /// Test that log events are collected for the categories and levels specified by the application.
        /// </summary>
        [ConditionalTheory(nameof(IsNotWindowsNetCore31))]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.NewlineDelimitedJson)]
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.NewlineDelimitedJson)]
#endif
        public Task LogsUseAppFiltersViaQueryTest(DiagnosticPortConnectionMode mode, LogFormat logFormat)
        {
            return ValidateLogsAsync(
                mode,
                logLevel: null,
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
        /// Test that log events are collected for the categories and levels specified by the application.
        /// </summary>
        [ConditionalTheory(nameof(IsNotWindowsNetCore31))]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.NewlineDelimitedJson)]
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.NewlineDelimitedJson)]
#endif
        public Task LogsUseAppFiltersViaBodyTest(DiagnosticPortConnectionMode mode, LogFormat logFormat)
        {
            return ValidateLogsAsync(
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
        [ConditionalTheory(nameof(IsNotWindowsNetCore31))]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.NewlineDelimitedJson)]
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.NewlineDelimitedJson)]
#endif
        public Task LogsUseAppFiltersAndFilterSpecsTest(DiagnosticPortConnectionMode mode, LogFormat logFormat)
        {
            return ValidateLogsAsync(
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
        [ConditionalTheory(nameof(IsNotWindowsNetCore31))]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.NewlineDelimitedJson)]
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.NewlineDelimitedJson)]
#endif
        public Task LogsWildcardTest(DiagnosticPortConnectionMode mode, LogFormat logFormat)
        {
            return ValidateLogsAsync(
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

        private Task ValidateLogsAsync(
            DiagnosticPortConnectionMode mode,
            LogLevel? logLevel,
            Func<ChannelReader<LogEntry>, Task> callback,
            LogFormat logFormat)
        {
            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                mode,
                TestAppScenarios.Logger.Name,
                appValidate: async (runner, client) =>
                    await ValidateResponseStream(
                        runner,
                        client.CaptureLogsAsync(
                            await runner.ProcessIdTask,
                            CommonTestTimeouts.LogsDuration,
                            logLevel,
                            logFormat),
                        callback,
                        logFormat));
        }

        private Task ValidateLogsAsync(
            DiagnosticPortConnectionMode mode,
            LogsConfiguration configuration,
            Func<ChannelReader<LogEntry>, Task> callback,
            LogFormat logFormat)
        {
            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                mode,
                TestAppScenarios.Logger.Name,
                appValidate: async (runner, client) =>
                    await ValidateResponseStream(
                        runner,
                        client.CaptureLogsAsync(
                            await runner.ProcessIdTask,
                            CommonTestTimeouts.LogsDuration,
                            configuration,
                            logFormat),
                        callback,
                        logFormat));
        }

        private async Task ValidateResponseStream(AppRunner runner, Task<ResponseStreamHolder> holderTask, Func<ChannelReader<LogEntry>, Task> callback, LogFormat logFormat)
        {
            Assert.NotNull(runner);
            Assert.NotNull(holderTask);
            Assert.NotNull(callback);

            // CONSIDER: Give dotnet-monitor some time to start the logs pipeline before having the target
            // application start logging. It would be best if dotnet-monitor could write a console event
            // (at Debug or Trace level) for when the pipeline has started. This would require dotnet-monitor
            // to know when the pipeline started and is waiting for logging data.
            await Task.Delay(TimeSpan.FromSeconds(3));

            // Start logging in the target application
            await runner.SendCommandAsync(TestAppScenarios.Logger.Commands.StartLogging);

            // Await the holder after sending the message to start logging so that ASP.NET can send chunked responses.
            // If awaited before sending the message, ASP.NET will not send the complete set of headers because no data
            // is written into the response stream. Since HttpClient.SendAsync has to wait for the complete set of headers,
            // the /logs invocation would run and complete with no log events. To avoid this, the /logs invocation is started,
            // then the StartLogging message is sent, and finally the holder is awaited.
            using ResponseStreamHolder holder = await holderTask;
            Assert.NotNull(holder);

            await LogsTestUtilities.ValidateLogsEquality(holder.Stream, callback, logFormat, _outputHelper);
        }

        public static bool IsNotWindowsNetCore31
        {
            get
            {
                // Skip logs tests for .NET Core 3.1 on Windows; these tests sporadically
                // fail frequently causing insertions and builds with unrelated changes to
                // fail. See https://github.com/dotnet/dotnet-monitor/issues/807 for details.
                return !TestConditions.IsWindows || !TestConditions.IsNetCore31;
            }
        }
    }
}
