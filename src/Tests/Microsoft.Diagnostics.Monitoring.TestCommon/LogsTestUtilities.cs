// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal static class LogsTestUtilities
    {
        const char JsonSequenceRecordSeparator = '\u001E';

        /// <summary>
        /// Validates each aspect of a <see cref="LogEntry"/> compared to the expected values
        /// on a reference <see cref="LogEntry"/> instance.
        /// </summary>
        /// <remarks>
        /// The Exception property of the <paramref name="expected"/> argument is compared
        /// to the first line of the Exception property of the <paramref name="actual"/> argument,
        /// which contains the exception name and message.
        /// </remarks>
        internal static void ValidateEntry(LogEntry expected, LogEntry actual)
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

        internal static async Task ValidateLogsEquality(Stream logsStream, Func<ChannelReader<LogEntry>, Task> callback, LogFormat logFormat, ITestOutputHelper outputHelper)
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

            outputHelper.WriteLine("Begin reading log entries.");
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

                outputHelper.WriteLine("Log entry: {0}", line);
                try
                {
                    LogEntry entry = JsonSerializer.Deserialize<LogEntry>(line, options);
                    if (null != entry)
                    {
                        // If the sentinel entry is encountered, stop processing more entries as
                        // any remaining entries are meant to mitigate event buffering in the runtime
                        // and trace event library.
                        if (entry.Category == TestAppScenarios.Logger.Categories.SentinelCategory)
                        {
                            break;
                        }
                        await channel.Writer.WriteAsync(entry);
                    }
                }
                catch (JsonException ex)
                {
                    outputHelper.WriteLine("Exception while deserializing log entry: {0}", ex);
                }
            }
            outputHelper.WriteLine("End reading log entries.");
            channel.Writer.Complete();

            await callbackTask;
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
                    { "{OriginalFormat}", "Trace message with values {Value1} and {Value2}." },
                    { "Message", "Trace message with values 3 and True." },
                    { "Value1", "3" },
                    { "Value2", "True" }
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
                    { "{OriginalFormat}", "Debug message with values {Value1} and {Value2}." },
                    { "Message", "Debug message with values f39a5065-732b-4cce-89d1-52e4af39e233 and (null)." },
                    { "Value1", "f39a5065-732b-4cce-89d1-52e4af39e233" },
                    { "Value2", "(null)" }
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
                    { "{OriginalFormat}", "Information message with values {Value1} and {Value2}." },
                    { "Message", "Information message with values hello and goodbye." },
                    { "Value1", "hello" },
                    { "Value2", "goodbye" }
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
                    { "{OriginalFormat}", "Error message with values {Value1} and {Value2}." },
                    { "Message", "Error message with values a and 42." },
                    { "Value1", "a" },
                    { "Value2", "42" }
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

        internal static readonly LogEntry Category1TraceEntry =
            CreateTraceEntry(TestAppScenarios.Logger.Categories.LoggerCategory1);

        internal static readonly LogEntry Category1DebugEntry =
            CreateDebugEntry(TestAppScenarios.Logger.Categories.LoggerCategory1);

        internal static readonly LogEntry Category1InformationEntry =
            CreateInformationEntry(TestAppScenarios.Logger.Categories.LoggerCategory1);

        internal static readonly LogEntry Category1WarningEntry =
            CreateWarningEntry(TestAppScenarios.Logger.Categories.LoggerCategory1);

        internal static readonly LogEntry Category1ErrorEntry =
            CreateErrorEntry(TestAppScenarios.Logger.Categories.LoggerCategory1);

        internal static readonly LogEntry Category1CriticalEntry =
            CreateCriticalEntry(TestAppScenarios.Logger.Categories.LoggerCategory1);

        internal static readonly LogEntry Category2DebugEntry =
            CreateDebugEntry(TestAppScenarios.Logger.Categories.LoggerCategory2);

        internal static readonly LogEntry Category2InformationEntry =
            CreateInformationEntry(TestAppScenarios.Logger.Categories.LoggerCategory2);

        internal static readonly LogEntry Category2WarningEntry =
            CreateWarningEntry(TestAppScenarios.Logger.Categories.LoggerCategory2);

        internal static readonly LogEntry Category2ErrorEntry =
            CreateErrorEntry(TestAppScenarios.Logger.Categories.LoggerCategory2);

        internal static readonly LogEntry Category2CriticalEntry =
            CreateCriticalEntry(TestAppScenarios.Logger.Categories.LoggerCategory2);

        internal static readonly LogEntry Category3TraceEntry =
            CreateTraceEntry(TestAppScenarios.Logger.Categories.LoggerCategory3);

        internal static readonly LogEntry Category3DebugEntry =
            CreateDebugEntry(TestAppScenarios.Logger.Categories.LoggerCategory3);

        internal static readonly LogEntry Category3InformationEntry =
            CreateInformationEntry(TestAppScenarios.Logger.Categories.LoggerCategory3);

        internal static readonly LogEntry Category3WarningEntry =
            CreateWarningEntry(TestAppScenarios.Logger.Categories.LoggerCategory3);

        internal static readonly LogEntry Category3ErrorEntry =
            CreateErrorEntry(TestAppScenarios.Logger.Categories.LoggerCategory3);

        internal static readonly LogEntry Category3CriticalEntry =
            CreateCriticalEntry(TestAppScenarios.Logger.Categories.LoggerCategory3);
    }
}
