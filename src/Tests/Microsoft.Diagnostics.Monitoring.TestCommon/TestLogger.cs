// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal record LogRecordEntry(EventId EventId, string Category, string Message, IList<IReadOnlyList<KeyValuePair<string, object>>> Scopes);

    internal sealed class ScopeTracker
    {
        public ConcurrentDictionary<int, Scope> Scopes { get; } = new();

        private int _lastHandle = -1;

        public int Register(Scope scope)
        {
            int handle = Interlocked.Increment(ref _lastHandle);
            Scopes[handle] = scope;
            return handle;
        }

        public void Remove(int handle)
        {
            Scopes.TryRemove(handle, out _);
        }
    }

    internal sealed class Scope : IDisposable
    {
        public object State { get; set; }
        private static int _handle;
        private readonly ScopeTracker _scopeTracker;

        public Scope(object state, ScopeTracker tracker)
        {
            State = state;
            _scopeTracker = tracker;
            _handle = _scopeTracker.Register(this);
        }

        public void Dispose()
        {
            _scopeTracker.Remove(_handle);
        }
    }

    internal sealed class LogRecord
    {
        private readonly List<LogRecordEntry> _events = new();

        public void Add(EventId id, string category, string message, ScopeTracker scopeTracker)
        {
            List<IReadOnlyList<KeyValuePair<string, object>>> keyValueScopes = new();
            foreach (Scope scope in scopeTracker.Scopes.Values)
            {
                if (scope.State is IReadOnlyList<KeyValuePair<string, object>> keyValueScope)
                {
                    keyValueScopes.Add(keyValueScope);
                }
            }
            _events.Add(new LogRecordEntry(id, category, message, keyValueScopes));
        }

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
        private readonly ScopeTracker _scopeTracker;

        public TestLogger(LogRecord record, string categoryName)
        {
            _logRecord = record;
            _categoryName = categoryName;
            _scopeTracker = new();
        }

        public IDisposable BeginScope<TState>(TState state) => new Scope(state, _scopeTracker);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _logRecord.Add(eventId, _categoryName, formatter(state, exception), _scopeTracker);
        }
    }
}
