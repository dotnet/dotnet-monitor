// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    internal sealed class TestOutputLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, TestOutputLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
        private readonly ITestOutputHelper _outputHelper;

        public TestOutputLoggerProvider(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, (name, helper) => new TestOutputLogger(helper, name), _outputHelper);
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }

    internal sealed class TestOutputLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly ITestOutputHelper _outputHelper;

        public TestOutputLogger(ITestOutputHelper outputHelper, string categoryName)
        {
            _categoryName = categoryName;
            _outputHelper = outputHelper;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _outputHelper.WriteLine($"[Logger:{_categoryName}][Id:{eventId.Id}] {formatter(state, exception)}");
        }
    }
}
