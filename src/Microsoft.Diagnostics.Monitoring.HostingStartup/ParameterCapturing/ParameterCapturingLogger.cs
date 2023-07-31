// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    internal sealed class ParameterCapturingLogger : IDisposable
    {
        private readonly ILogger _userLogger;
        private readonly ILogger _systemLogger;
        private readonly Thread _thread;
        private BlockingCollection<(string format, string[] args)> _messages;
        private uint _droppedMessageCounter;
        private const int BackgroundLoggingCapacity = 1024;
        private const string BackgroundLoggingThreadName = "Probe Logging Thread";
        private long _disposedState;

        private static readonly string[] ExcludedThreads = new[]
        {
            "Console logger queue processing thread",
        };

        public ParameterCapturingLogger(ILogger userLogger, ILogger systemLogger)
        {
            _userLogger = userLogger;
            _systemLogger = systemLogger;
            _thread = new Thread(ThreadProc);

            _thread.Priority = ThreadPriority.BelowNormal;
            _thread.Name = BackgroundLoggingThreadName;
            _messages = new BlockingCollection<(string, string[])>(BackgroundLoggingCapacity);
            _thread.Start();
        }

        public bool ShouldLog()
        {
            // Probes should not attempt to log on the console logging thread
            // or on the background thread that is used to log system messages.

            if (Environment.CurrentManagedThreadId == _thread.ManagedThreadId)
            {
                return false;
            }
            if (ExcludedThreads.Contains(Thread.CurrentThread.Name))
            {
                return false;
            }

            return true;
        }

        public void Log(ParameterCaptureMode mode, string format, string[] args)
        {
            if (mode == ParameterCaptureMode.Inline)
            {
                Log(_userLogger, format, args);
            }
            else if (mode == ParameterCaptureMode.Background)
            {
                if (!_messages.TryAdd((format, args)))
                {
                    _droppedMessageCounter++;
                }
            }
        }

        private void ThreadProc()
        {
            while (!DisposableHelper.IsDisposed(ref _disposedState))
            {
                (string format, string[] args) = _messages.Take();
                Log(_systemLogger, format, args);
            }
        }

        private static void Log(ILogger logger, string format, string[] args) => logger.Log(LogLevel.Information, format, args);

        public void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
            {
                return;
            }
        }
    }
}
