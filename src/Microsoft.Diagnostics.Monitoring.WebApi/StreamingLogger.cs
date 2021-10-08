﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        private readonly ScopeState _scopes = new ScopeState();
        private readonly Stream _outputStream;
        private readonly string _categoryName;
        private readonly LogFormat _logFormat;
        private readonly LogLevel? _logLevel;

        public const byte JsonSequenceRecordSeparator = 0x1E;

        public StreamingLogger(string category, Stream outputStream, LogFormat format, LogLevel? logLevel)
        {
            _outputStream = outputStream;
            _categoryName = category;
            _logFormat = format;
            _logLevel = logLevel;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            if (state is LogObject logObject)
            {
                return _scopes.Push(logObject);
            }
            return null;
        }

        public bool IsEnabled(LogLevel logLevel) => (_logLevel == null) ? true : logLevel <= _logLevel.Value;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
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
                LogEventStream(logLevel, eventId, state, exception, formatter);
            }
        }

        private void LogJson<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter, LogFormat jsonFormat = LogFormat.NewlineDelimitedJson)
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

        private void LogEventStream<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Stream outputStream = _outputStream;

            // Matches the format of SimpleConsoleFormatter as much as possible

            using var writer = new StreamWriter(outputStream, Encoding.UTF8, 1024, leaveOpen: true) { NewLine = "\n" };

            //event: eventName (if exists)
            //data: level category[eventId]
            //data: timestamp
            //data: message
            //data: => scope1, scope2 => scope3, scope4
            //data: exception (if exists)
            //\n

            if (!string.IsNullOrEmpty(eventId.Name))
            {
                writer.Write("event: ");
                writer.WriteLine(eventId.Name);
            }
            writer.Write("data: ");
            writer.Write(logLevel);
            writer.Write(" ");
            writer.Write(_categoryName);
            writer.Write('[');
            writer.Write(eventId.Id);
            writer.WriteLine(']');
            if (state is IStateWithTimestamp stateWithTimestamp)
            {
                writer.Write("data: ");
                writer.Write(FormatTimestamp(stateWithTimestamp));
                writer.WriteLine();
            }

            writer.Write("data: ");
            writer.WriteLine(formatter(state, exception));

            // Scopes
            bool firstScope = true;
            foreach (IReadOnlyList<KeyValuePair<string, object>> scope in _scopes)
            {
                bool firstScopeEntry = true;
                foreach (KeyValuePair<string, object> scopeValue in scope)
                {
                    if (firstScope)
                    {
                        writer.Write("data:");
                        firstScope = false;
                    }

                    if (firstScopeEntry)
                    {
                        writer.Write(" => ");
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
            if (!firstScope)
            {
                writer.WriteLine();
            }

            // Exception
            if (null != exception)
            {
                writer.Write("data: ");
                writer.WriteLine(exception.ToString().Replace(Environment.NewLine, $"{writer.NewLine}data: "));
            }

            writer.WriteLine();
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
    }
}
