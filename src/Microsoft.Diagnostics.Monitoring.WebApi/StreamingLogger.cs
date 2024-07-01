// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// This class is used to write structured event data in json format to an output stream.
    /// </summary>
    public sealed class StreamingLoggerProvider : ILoggerProvider
    {
        private readonly Stream _outputStream;
        private readonly LogFormat _format;
        private readonly LogLevel? _logLevel;

        public StreamingLoggerProvider(Stream outputStream, LogFormat logFormat, LogLevel? logLevel)
        {
            _outputStream = outputStream;
            _format = logFormat;
            _logLevel = logLevel;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new StreamingLogger(categoryName, _outputStream, _format, _logLevel);
        }

        public void Dispose()
        {
        }
    }

    public sealed class StreamingLogger : ILogger
    {
        private sealed class EmptyScope : IDisposable
        {
            public void Dispose()
            {
            }
        }

        private readonly ScopeState _scopes = new ScopeState();
        private readonly Stream _outputStream;
        private readonly string _categoryName;
        private readonly LogFormat _logFormat;
        private readonly LogLevel? _logLevel;

        // This is the padding used for each non-header line of the logs output when formatted as plain text.
        // Since the plain text output adheres to the same format as simple console formatter, the same
        // padding algorithm is used here. This padding specifically allows all of the text (including the
        // header without the level information and without using timestamps) to be vertically aligned.
        //
        // For example:
        //       | All non-level information aligned here when not using timestamps.
        //       v
        // info: LoggerCategory[0]
        //       LoggerMessage
        private static readonly int PlainTextPaddingLength = GetLogLevelString(LogLevel.Information).Length + ": ".Length;
        private static readonly string PlainTextPadding = new string(' ', PlainTextPaddingLength);

        public const byte JsonSequenceRecordSeparator = 0x1E;

        public StreamingLogger(string category, Stream outputStream, LogFormat format, LogLevel? logLevel)
        {
            _outputStream = outputStream;
            _categoryName = category;
            _logFormat = format;
            _logLevel = logLevel;
        }

        IDisposable ILogger.BeginScope<TState>(TState state)
        {
            if (state is LogObject logObject)
            {
                return _scopes.Push(logObject);
            }
            return new EmptyScope();
        }

        public bool IsEnabled(LogLevel logLevel) => (_logLevel == null) ? true : logLevel <= _logLevel.Value;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (_logFormat == LogFormat.NewlineDelimitedJson)
            {
                LogJson(logLevel, eventId, state, exception, formatter);
            }
            else if (_logFormat == LogFormat.JsonSequence)
            {
                LogJson(logLevel, eventId, state, exception, formatter, LogFormat.JsonSequence);
            }
            else
            {
                LogText(logLevel, eventId, state, exception, formatter);
            }
        }

        private void LogJson<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter, LogFormat jsonFormat = LogFormat.NewlineDelimitedJson)
        {
            Stream outputStream = _outputStream;

            if (jsonFormat == LogFormat.JsonSequence)
            {
                outputStream.WriteByte(JsonSequenceRecordSeparator);
            }

            //CONSIDER Should we cache up the loggers and writers?
            using (var jsonWriter = new Utf8JsonWriter(outputStream, new JsonWriterOptions { Indented = false }))
            {
                // Matches the format of JsonConsoleFormatter

                jsonWriter.WriteStartObject();
                jsonWriter.WriteString("Timestamp", (state is IStateWithTimestamp stateWithTimestamp) ? FormatTimestamp(stateWithTimestamp) : string.Empty);
                jsonWriter.WriteString("LogLevel", logLevel.ToString());
                jsonWriter.WriteNumber("EventId", eventId.Id);
                // EventId.Name is optional; use empty string if it is null as this
                // works better with analytic platforms such as Azure Monitor.
                jsonWriter.WriteString("EventName", eventId.Name ?? string.Empty);
                jsonWriter.WriteString("Category", _categoryName);
                jsonWriter.WriteString("Message", formatter(state, exception));
                if (exception != null)
                {
                    jsonWriter.WriteString("Exception", exception.ToString());
                }

                // Write out state
                if (state is IEnumerable<KeyValuePair<string, object>> values)
                {
                    jsonWriter.WriteStartObject("State");
                    jsonWriter.WriteString("Message", state.ToString());
                    foreach (KeyValuePair<string, object> arg in values)
                    {
                        WriteKeyValuePair(jsonWriter, arg);
                    }
                    jsonWriter.WriteEndObject();
                }

                // Write out scopes
                if (_scopes.HasScopes)
                {
                    jsonWriter.WriteStartArray("Scopes");
                    foreach (IReadOnlyList<KeyValuePair<string, object>> scope in _scopes)
                    {
                        jsonWriter.WriteStartObject();
                        foreach (KeyValuePair<string, object> scopeValue in scope)
                        {
                            WriteKeyValuePair(jsonWriter, scopeValue);
                        }
                        jsonWriter.WriteEndObject();
                    }
                    jsonWriter.WriteEndArray();
                }

                jsonWriter.WriteEndObject();
                jsonWriter.Flush();
            }

            // JSON Sequence and NDJson both use newline as the end character
            outputStream.WriteByte((byte)'\n');

            outputStream.Flush();
        }

        private void LogText<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Stream outputStream = _outputStream;

            // Matches the format of SimpleConsoleFormatter as much as possible

            using var writer = new StreamWriter(outputStream, EncodingCache.UTF8NoBOMNoThrow, 1024, leaveOpen: true) { NewLine = "\n" };

            // Format (based on simple console format):
            // Note: This deviates slightly from the simple console format in that the event name
            // is also logged as a suffix on the first line whereas the simple console format does
            // not log the event name at all.

            // Timestamp Level: Category[EventId][EventName]
            //       => Scope1Name1:Scope1Value1, Scope1Name2:Scope1Value2 => Scope2Name1:Scope2Value2
            //       Message
            //       Exception

            // Timestamp
            if (state is IStateWithTimestamp stateWithTimestamp)
            {
                writer.Write(FormatTimestamp(stateWithTimestamp));
                writer.Write(" ");
            }
            writer.Write(GetLogLevelString(logLevel));
            writer.Write(": ");
            writer.Write(_categoryName);
            writer.Write('[');
            writer.Write(eventId.Id);
            writer.Write(']');
            if (!string.IsNullOrEmpty(eventId.Name))
            {
                writer.Write('[');
                writer.Write(eventId.Name);
                writer.Write(']');
            }
            writer.WriteLine();

            // Scopes
            if (_scopes.HasScopes)
            {
                writer.Write(PlainTextPadding);
                bool firstScope = true;
                foreach (IReadOnlyList<KeyValuePair<string, object>> scope in _scopes)
                {
                    // The first scope should not have extra padding before the delimiter since
                    // it was already padded by the padding added for every line.
                    if (firstScope)
                    {
                        firstScope = false;
                    }
                    else
                    {
                        writer.Write(" ");
                    }
                    writer.Write("=> ");

                    bool firstScopeEntry = true;
                    foreach (KeyValuePair<string, object> scopeValue in scope)
                    {
                        if (firstScopeEntry)
                        {
                            firstScopeEntry = false;
                        }
                        else
                        {
                            writer.Write(", ");
                        }
                        writer.Write(scopeValue.Key);
                        writer.Write(':');
                        writer.Write(scopeValue.Value);
                    }
                }
                writer.WriteLine();
            }

            // Message
            writer.Write(PlainTextPadding);
            writer.WriteLine(formatter(state, exception));

            // Exception
            if (null != exception)
            {
                writer.Write(PlainTextPadding);
                writer.WriteLine(exception.ToString().Replace(Environment.NewLine, writer.NewLine + PlainTextPadding));
            }
        }

        private static string FormatTimestamp(IStateWithTimestamp stateWithTimestamp)
        {
            // "u" Universal time with sortable format, "yyyy'-'MM'-'dd HH':'mm':'ss'Z'" 1999-10-31 10:00:00Z
            // based on ISO 8601.
            return stateWithTimestamp.Timestamp.ToUniversalTime().ToString("u");
        }

        private static void WriteKeyValuePair(Utf8JsonWriter jsonWriter, KeyValuePair<string, object> kvp)
        {
            jsonWriter.WritePropertyName(kvp.Key);
            switch (kvp.Value)
            {
                case string s:
                    jsonWriter.WriteStringValue(s);
                    break;
                case int i:
                    jsonWriter.WriteNumberValue(i);
                    break;
                case bool b:
                    jsonWriter.WriteBooleanValue(b);
                    break;
                case null:
                    jsonWriter.WriteNullValue();
                    break;
                default:
                    jsonWriter.WriteStringValue(kvp.Value.ToString());
                    break;
            }
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "trce",
                LogLevel.Debug => "dbug",
                LogLevel.Information => "info",
                LogLevel.Warning => "warn",
                LogLevel.Error => "fail",
                LogLevel.Critical => "crit",
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
            };
        }
    }
}
