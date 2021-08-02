// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Models;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using DiagnosticPortConnectionMode = Microsoft.Diagnostics.Monitoring.TestCommon.Options.DiagnosticPortConnectionMode;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [Collection(DefaultCollectionFixture.Name)]
    public class LogsTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        const char JsonSequenceRecordSeparator = '\u001E';

        public LogsTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Tests that all log events are collected if log level set to Trace.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.NDJson)]

#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.NDJson)]
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

                    //ValidateEntry(Category1TraceEntry, await reader.ReadAsync());
                    ValidateEntry(Category1DebugEntry, await reader.ReadAsync());
                    ValidateEntry(Category1InformationEntry, await reader.ReadAsync());
                    ValidateEntry(Category1WarningEntry, await reader.ReadAsync());
                    ValidateEntry(Category1ErrorEntry, await reader.ReadAsync());
                    ValidateEntry(Category1CriticalEntry, await reader.ReadAsync());
                    //ValidateEntry(Category2TraceEntry, await reader.ReadAsync());
                    ValidateEntry(Category2DebugEntry, await reader.ReadAsync());
                    ValidateEntry(Category2InformationEntry, await reader.ReadAsync());
                    ValidateEntry(Category2WarningEntry, await reader.ReadAsync());
                    ValidateEntry(Category2ErrorEntry, await reader.ReadAsync());
                    ValidateEntry(Category2CriticalEntry, await reader.ReadAsync());
                    //ValidateEntry(Category3TraceEntry, await reader.ReadAsync());
                    ValidateEntry(Category3DebugEntry, await reader.ReadAsync());
                    ValidateEntry(Category3InformationEntry, await reader.ReadAsync());
                    ValidateEntry(Category3WarningEntry, await reader.ReadAsync());
                    ValidateEntry(Category3ErrorEntry, await reader.ReadAsync());
                    ValidateEntry(Category3CriticalEntry, await reader.ReadAsync());
                    Assert.False(await reader.WaitToReadAsync());
                },
                logFormat);
        }

        /// <summary>
        /// Tests that log events with level at or above the specified level are collected.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.NDJson)]
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.NDJson)]
#endif
        public Task LogsDefaultLevelTest(DiagnosticPortConnectionMode mode, LogFormat logFormat)
        {
            return ValidateLogsAsync(
                mode,
                LogLevel.Warning,
                async reader =>
                {
                    ValidateEntry(Category1WarningEntry, await reader.ReadAsync());
                    ValidateEntry(Category1ErrorEntry, await reader.ReadAsync());
                    ValidateEntry(Category1CriticalEntry, await reader.ReadAsync());
                    ValidateEntry(Category2WarningEntry, await reader.ReadAsync());
                    ValidateEntry(Category2ErrorEntry, await reader.ReadAsync());
                    ValidateEntry(Category2CriticalEntry, await reader.ReadAsync());
                    ValidateEntry(Category3WarningEntry, await reader.ReadAsync());
                    ValidateEntry(Category3ErrorEntry, await reader.ReadAsync());
                    ValidateEntry(Category3CriticalEntry, await reader.ReadAsync());
                    Assert.False(await reader.WaitToReadAsync());
                },
                logFormat);
        }

        /// <summary>
        /// Test that log events with a category that doesn't have a specified level are collected
        /// at the log level specified in the request body.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.NDJson)]
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.NDJson)]
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
                    ValidateEntry(Category1ErrorEntry, await reader.ReadAsync());
                    ValidateEntry(Category1CriticalEntry, await reader.ReadAsync());
                    ValidateEntry(Category2InformationEntry, await reader.ReadAsync());
                    ValidateEntry(Category2WarningEntry, await reader.ReadAsync());
                    ValidateEntry(Category2ErrorEntry, await reader.ReadAsync());
                    ValidateEntry(Category2CriticalEntry, await reader.ReadAsync());
                    ValidateEntry(Category3WarningEntry, await reader.ReadAsync());
                    ValidateEntry(Category3ErrorEntry, await reader.ReadAsync());
                    ValidateEntry(Category3CriticalEntry, await reader.ReadAsync());
                    Assert.False(await reader.WaitToReadAsync());
                },
                logFormat);
        }

        /// <summary>
        /// Test that LogLevel.None is not supported as the level query parameter.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.NDJson)]
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.NDJson)]
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
                                runner.ProcessId,
                                TestTimeouts.LogsDuration,
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
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.NDJson)]
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.NDJson)]
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
                                runner.ProcessId,
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

        /// <summary>
        /// Test that log events are collected for the categories and levels specified by the application.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.NDJson)]
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.NDJson)]
#endif
        public Task LogsUseAppFiltersViaQueryTest(DiagnosticPortConnectionMode mode, LogFormat logFormat)
        {
            return ValidateLogsAsync(
                mode,
                logLevel: null,
                async reader =>
                {
                    ValidateEntry(Category1DebugEntry, await reader.ReadAsync());
                    ValidateEntry(Category1InformationEntry, await reader.ReadAsync());
                    ValidateEntry(Category1WarningEntry, await reader.ReadAsync());
                    ValidateEntry(Category1ErrorEntry, await reader.ReadAsync());
                    ValidateEntry(Category1CriticalEntry, await reader.ReadAsync());
                    ValidateEntry(Category2InformationEntry, await reader.ReadAsync());
                    ValidateEntry(Category2WarningEntry, await reader.ReadAsync());
                    ValidateEntry(Category2ErrorEntry, await reader.ReadAsync());
                    ValidateEntry(Category2CriticalEntry, await reader.ReadAsync());
                    ValidateEntry(Category3WarningEntry, await reader.ReadAsync());
                    ValidateEntry(Category3ErrorEntry, await reader.ReadAsync());
                    ValidateEntry(Category3CriticalEntry, await reader.ReadAsync());
                    Assert.False(await reader.WaitToReadAsync());
                },
                logFormat);
        }

        /// <summary>
        /// Test that log events are collected for the categories and levels specified by the application.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.NDJson)]
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.NDJson)]
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
                    ValidateEntry(Category1DebugEntry, await reader.ReadAsync());
                    ValidateEntry(Category1InformationEntry, await reader.ReadAsync());
                    ValidateEntry(Category1WarningEntry, await reader.ReadAsync());
                    ValidateEntry(Category1ErrorEntry, await reader.ReadAsync());
                    ValidateEntry(Category1CriticalEntry, await reader.ReadAsync());
                    ValidateEntry(Category2InformationEntry, await reader.ReadAsync());
                    ValidateEntry(Category2WarningEntry, await reader.ReadAsync());
                    ValidateEntry(Category2ErrorEntry, await reader.ReadAsync());
                    ValidateEntry(Category2CriticalEntry, await reader.ReadAsync());
                    ValidateEntry(Category3WarningEntry, await reader.ReadAsync());
                    ValidateEntry(Category3ErrorEntry, await reader.ReadAsync());
                    ValidateEntry(Category3CriticalEntry, await reader.ReadAsync());
                    Assert.False(await reader.WaitToReadAsync());
                },
                logFormat);
        }

        /// <summary>
        /// Test that log events are collected for the categories and levels specified by the application
        /// and for the categories and levels specified in the filter specs.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.NDJson)]
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.NDJson)]
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
                    ValidateEntry(Category1DebugEntry, await reader.ReadAsync());
                    ValidateEntry(Category1InformationEntry, await reader.ReadAsync());
                    ValidateEntry(Category1WarningEntry, await reader.ReadAsync());
                    ValidateEntry(Category1ErrorEntry, await reader.ReadAsync());
                    ValidateEntry(Category1CriticalEntry, await reader.ReadAsync());
                    ValidateEntry(Category2InformationEntry, await reader.ReadAsync());
                    ValidateEntry(Category2WarningEntry, await reader.ReadAsync());
                    ValidateEntry(Category2ErrorEntry, await reader.ReadAsync());
                    ValidateEntry(Category2CriticalEntry, await reader.ReadAsync());
                    ValidateEntry(Category3DebugEntry, await reader.ReadAsync());
                    ValidateEntry(Category3InformationEntry, await reader.ReadAsync());
                    ValidateEntry(Category3WarningEntry, await reader.ReadAsync());
                    ValidateEntry(Category3ErrorEntry, await reader.ReadAsync());
                    ValidateEntry(Category3CriticalEntry, await reader.ReadAsync());
                    Assert.False(await reader.WaitToReadAsync());
                },
                logFormat);
        }

        /// <summary>
        /// Test that log events are collected for wildcard categories.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Connect, LogFormat.NDJson)]
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.JsonSequence)]
        [InlineData(DiagnosticPortConnectionMode.Listen, LogFormat.NDJson)]
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
                    ValidateEntry(Category1TraceEntry, await reader.ReadAsync());
                    ValidateEntry(Category1DebugEntry, await reader.ReadAsync());
                    ValidateEntry(Category1InformationEntry, await reader.ReadAsync());
                    ValidateEntry(Category1WarningEntry, await reader.ReadAsync());
                    ValidateEntry(Category1ErrorEntry, await reader.ReadAsync());
                    ValidateEntry(Category1CriticalEntry, await reader.ReadAsync());
                    ValidateEntry(Category2WarningEntry, await reader.ReadAsync());
                    ValidateEntry(Category2ErrorEntry, await reader.ReadAsync());
                    ValidateEntry(Category2CriticalEntry, await reader.ReadAsync());
                    ValidateEntry(Category3TraceEntry, await reader.ReadAsync());
                    ValidateEntry(Category3DebugEntry, await reader.ReadAsync());
                    ValidateEntry(Category3InformationEntry, await reader.ReadAsync());
                    ValidateEntry(Category3WarningEntry, await reader.ReadAsync());
                    ValidateEntry(Category3ErrorEntry, await reader.ReadAsync());
                    ValidateEntry(Category3CriticalEntry, await reader.ReadAsync());
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
                appValidate: (runner, client) => ValidateResponseStream(
                    runner,
                    client.CaptureLogsAsync(runner.ProcessId, TestTimeouts.LogsDuration, logLevel, logFormat),
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
                appValidate: (runner, client) => ValidateResponseStream(
                    runner,
                    client.CaptureLogsAsync(runner.ProcessId, TestTimeouts.LogsDuration, configuration, logFormat),
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

            using StreamReader reader = new StreamReader(holder.Stream);

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

        /// <summary>
        /// Validates each aspect of a <see cref="LogEntry"/> compared to the expected values
        /// on a reference <see cref="LogEntry"/> instance.
        /// </summary>
        /// <remarks>
        /// The Exception property of the <paramref name="expected"/> argument is compared
        /// to the first line of the Exception property of the <paramref name="actual"/> argument,
        /// which contains the exception name and message.
        /// </remarks>
        private static void ValidateEntry(LogEntry expected, LogEntry actual)
        {
            Assert.Equal(expected.Category, actual.Category);

            Assert.Equal(expected.EventId, actual.EventId);
            
            Assert.Equal(expected.EventName, actual.EventName);

            if (null == expected.Exception)
            {
                Assert.Null(actual.Exception);
            }
            else
            {
                Assert.NotNull(actual.Exception);
                // Only compare the first line, which contains the exception type and message.
                // The other lines contain callstack information, which will vary between machines.
                string firstLine = actual.Exception.Split(Environment.NewLine)[0];
                Assert.Equal(expected.Exception, firstLine);
            }

            Assert.Equal(expected.LogLevel, actual.LogLevel);

            Assert.Equal(expected.Message, actual.Message);

            // TODO: Work on scope verification
            if (null == expected.Scopes)
            {
                Assert.Null(actual.Scopes);
            }
            else
            {
                Assert.NotNull(actual.Scopes);
                Assert.Equal(expected.Scopes.Count, actual.Scopes.Count);
                foreach (string expectedKey in expected.Scopes.Keys)
                {
                    Assert.True(actual.Scopes.TryGetValue(expectedKey, out JsonElement? actualValue), $"Expected Scopes to contain '{expectedKey}' key.");
                    Assert.Equal(expected.Scopes[expectedKey], actualValue);
                }
            }

            Assert.NotNull(actual.State);
            Assert.Equal(expected.State.Count, actual.State.Count);
            foreach (string expectedKey in expected.State.Keys)
            {
                Assert.True(actual.State.TryGetValue(expectedKey, out string actualValue), $"Expected State to contain '{expectedKey}' key.");
                Assert.Equal(expected.State[expectedKey], actualValue);
            }
        }

        private static LogEntry CreateTraceEntry(string category)
        {
            return new LogEntry()
            {
                Category = category,
                EventId = 1,
                EventName = "EventIdTrace",
                Exception = null,
                LogLevel = LogLevel.Trace,
                Message = "Trace message with values 3 and True.",
                State = new Dictionary<string, string>()
                {
                    { "{OriginalFormat}", "Trace message with values {value1} and {value2}." },
                    { "Message", "Trace message with values 3 and True." },
                    { "value1", "3" },
                    { "value2", "True" }
                }
            };
        }

        private static LogEntry CreateDebugEntry(string category)
        {
            return new LogEntry()
            {
                Category = category,
                EventId = 1,
                EventName = "EventIdDebug",
                Exception = null,
                LogLevel = LogLevel.Debug,
                Message = "Debug message with values f39a5065-732b-4cce-89d1-52e4af39e233 and (null).",
                State = new Dictionary<string, string>()
                {
                    { "{OriginalFormat}", "Debug message with values {value1} and {value2}." },
                    { "Message", "Debug message with values f39a5065-732b-4cce-89d1-52e4af39e233 and (null)." },
                    { "value1", "f39a5065-732b-4cce-89d1-52e4af39e233" },
                    { "value2", "(null)" }
                }
            };
        }

        private static LogEntry CreateInformationEntry(string category)
        {
            return new LogEntry()
            {
                Category = category,
                EventId = 0,
                EventName = string.Empty,
                Exception = null,
                LogLevel = LogLevel.Information,
                Message = "Information message with values hello and goodbye.",
                State = new Dictionary<string, string>()
                {
                    { "{OriginalFormat}", "Information message with values {value1} and {value2}." },
                    { "Message", "Information message with values hello and goodbye." },
                    { "value1", "hello" },
                    { "value2", "goodbye" }
                }
            };
        }

        private static LogEntry CreateWarningEntry(string category)
        {
            return new LogEntry()
            {
                Category = category,
                EventId = 5,
                EventName = "EventIdWarning",
                Exception = null,
                LogLevel = LogLevel.Warning,
                Message = "'Warning message with custom state.' with 3 state values.",
                State = new Dictionary<string, string>()
                {
                    { "Message", "'Warning message with custom state.' with 3 state values." },
                    { "KeyA", "4" },
                    { "Key2", "p" },
                    { "KeyZ", "Error" }
                }
            };
        }

        private static LogEntry CreateErrorEntry(string category)
        {
            return new LogEntry()
            {
                Category = category,
                EventId = 1,
                EventName = "EventIdError",
                Exception = null,
                LogLevel = LogLevel.Error,
                Message = "Error message with values a and 42.",
                State = new Dictionary<string, string>()
                {
                    { "{OriginalFormat}", "Error message with values {value1} and {value2}." },
                    { "Message", "Error message with values a and 42." },
                    { "value1", "a" },
                    { "value2", "42" }
                }
            };
        }

        private static LogEntry CreateCriticalEntry(string category)
        {
            return new LogEntry()
            {
                Category = category,
                EventId = 1,
                EventName = "EventIdCritical",
                Exception = "System.InvalidOperationException: Application is shutting down.",
                LogLevel = LogLevel.Critical,
                Message = "Critical message.",
                State = new Dictionary<string, string>()
                {
                    { "{OriginalFormat}", "Critical message." },
                    { "Message", "Critical message." },
                }
            };
        }

        private static readonly LogEntry Category1TraceEntry =
            CreateTraceEntry(TestAppScenarios.Logger.Categories.LoggerCategory1);

        private static readonly LogEntry Category1DebugEntry =
            CreateDebugEntry(TestAppScenarios.Logger.Categories.LoggerCategory1);

        private static readonly LogEntry Category1InformationEntry =
            CreateInformationEntry(TestAppScenarios.Logger.Categories.LoggerCategory1);

        private static readonly LogEntry Category1WarningEntry =
            CreateWarningEntry(TestAppScenarios.Logger.Categories.LoggerCategory1);

        private static readonly LogEntry Category1ErrorEntry =
            CreateErrorEntry(TestAppScenarios.Logger.Categories.LoggerCategory1);

        private static readonly LogEntry Category1CriticalEntry =
            CreateCriticalEntry(TestAppScenarios.Logger.Categories.LoggerCategory1);

        private static readonly LogEntry Category2DebugEntry =
            CreateDebugEntry(TestAppScenarios.Logger.Categories.LoggerCategory2);

        private static readonly LogEntry Category2InformationEntry =
            CreateInformationEntry(TestAppScenarios.Logger.Categories.LoggerCategory2);

        private static readonly LogEntry Category2WarningEntry =
            CreateWarningEntry(TestAppScenarios.Logger.Categories.LoggerCategory2);

        private static readonly LogEntry Category2ErrorEntry =
            CreateErrorEntry(TestAppScenarios.Logger.Categories.LoggerCategory2);

        private static readonly LogEntry Category2CriticalEntry =
            CreateCriticalEntry(TestAppScenarios.Logger.Categories.LoggerCategory2);

        private static readonly LogEntry Category3TraceEntry =
            CreateTraceEntry(TestAppScenarios.Logger.Categories.LoggerCategory3);

        private static readonly LogEntry Category3DebugEntry =
            CreateDebugEntry(TestAppScenarios.Logger.Categories.LoggerCategory3);

        private static readonly LogEntry Category3InformationEntry =
            CreateInformationEntry(TestAppScenarios.Logger.Categories.LoggerCategory3);

        private static readonly LogEntry Category3WarningEntry =
            CreateWarningEntry(TestAppScenarios.Logger.Categories.LoggerCategory3);

        private static readonly LogEntry Category3ErrorEntry =
            CreateErrorEntry(TestAppScenarios.Logger.Categories.LoggerCategory3);

        private static readonly LogEntry Category3CriticalEntry =
            CreateCriticalEntry(TestAppScenarios.Logger.Categories.LoggerCategory3);
    }
}
