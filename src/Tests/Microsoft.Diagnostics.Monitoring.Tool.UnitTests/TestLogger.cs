// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    internal sealed class LogRecord
    {
        private readonly List<(EventId EventId, string Message)> _events = new();

        public void Add(EventId id, string message) => _events.Add((id, message));

        public IList<(EventId EventId, string Message)> Events => _events;
    }

    internal class TestLoggerProvider : ILoggerProvider
    {
        private readonly LogRecord _logRecord;

        public TestLoggerProvider(LogRecord record) => _logRecord = record;

        public ILogger CreateLogger(string categoryName) => new TestLogger(_logRecord, categoryName);

        public void Dispose() { }
    }

    internal class TestLogger : ILogger
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
            _logRecord.Add(eventId, formatter(state, exception));
        }
    }
}
