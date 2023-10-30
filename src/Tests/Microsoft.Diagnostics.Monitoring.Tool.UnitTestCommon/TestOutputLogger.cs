// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
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

        public ILogger<T> CreateLogger<T>()
        {
            return (ILogger<T>)_loggers.GetOrAdd(nameof(T), (name, helper) => new TestOutputLogger<T>(helper), _outputHelper);
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }

    internal sealed class TestOutputLogger<T> : TestOutputLogger, ILogger<T>
    {
        public TestOutputLogger(ITestOutputHelper outputHelper) : base(outputHelper, typeof(T).Name)
        {
        }
    }

    internal class TestOutputLogger : ILogger
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
            WriteLine(formatter(state, exception));
            if (null != exception)
            {
                WriteLine($"Exception: {exception.GetType().Name}");
                WriteLine($"Message: {exception.Message}");
                WriteLine("Start Stack");
                StringReader reader = new(exception.StackTrace);
                string line;
                while (null != (line = reader.ReadLine()))
                {
                    WriteLine(line);
                }
                WriteLine("End Stack");
            }

            void WriteLine(string text)
            {
                _outputHelper.WriteLine($"[Logger:{_categoryName}][Id:{eventId.Id}] {text}");
            }
        }
    }
}
