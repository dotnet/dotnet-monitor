// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal record LogRecordEntry(EventId EventId, string Category, string Message);

    internal sealed class LogRecord
    {
        private readonly List<LogRecordEntry> _events = new();

        public void Add(EventId id, string category, string message) => _events.Add(new LogRecordEntry(id, category, message));

        public IList<LogRecordEntry> Events => _events;
    }

    internal sealed class TestLoggerProvider : ILoggerProvider
    {
        private readonly LogRecord _logRecord;

        public TestLoggerProvider(LogRecord record) => _logRecord = record;

        public ILogger CreateLogger(string categoryName) => new TestLogger(_logRecord, categoryName);

        public void Dispose() { }
    }

    internal sealed class TestLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly LogRecord _logRecord;

        private sealed class Scope : IDisposable
        {
            public void Dispose() { }
        }

        public TestLogger(LogRecord record, string categoryName)
        {
            _logRecord = record;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) => new Scope();

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _logRecord.Add(eventId, _categoryName, formatter(state, exception));
        }
    }
}
