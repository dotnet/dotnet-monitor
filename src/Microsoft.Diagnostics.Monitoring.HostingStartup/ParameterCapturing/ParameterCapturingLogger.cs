// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes;
using Microsoft.Diagnostics.Monitoring.StartupHook;
using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline.Steps;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    internal sealed class ParameterCapturingLogger : IDisposable
    {
        private record QueuedLogStatement(string Format, string[] Args, KeyValueLogScope Scope);

        internal static class Scopes
        {
            private const string Prefix = "DotnetMonitor_";

            public const string TimeStamp = Prefix + "Timestamp";

            public const string ThreadId = Prefix + "ThreadId";

            public const string ActivityId = Prefix + "ActivityId";
            public const string ActivityIdFormat = Prefix + "ActivityIdFormat";

            public static class CaptureSite
            {
                private const string Prefix = Scopes.Prefix + "CaptureSite_";

                public const string MethodName = Prefix + "MethodName";
                public const string ModuleName = Prefix + "ModuleName";
                public const string TypeName = Prefix + "TypeName";
            }
        }

        private readonly ILogger _userLogger;
        private readonly ILogger _systemLogger;
        private readonly Thread _thread;
        private BlockingCollection<QueuedLogStatement> _messages;
        private uint _droppedMessageCounter;
        private const int BackgroundLoggingCapacity = 1024;
        private const string BackgroundLoggingThreadName = "[dotnet-monitor] Probe Logging Thread";
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
            _thread.IsBackground = true;
            _thread.Name = BackgroundLoggingThreadName;
            _messages = new BlockingCollection<QueuedLogStatement>(BackgroundLoggingCapacity);
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

        public void Log(ParameterCaptureMode mode, MethodTemplateString methodTemplateString, string[] args)
        {
            DisposableHelper.ThrowIfDisposed<ParameterCapturingLogger>(ref _disposedState);

            KeyValueLogScope scope = GenerateScope(methodTemplateString);

            if (mode == ParameterCaptureMode.Inline)
            {
                Log(_userLogger, methodTemplateString.Template, args, scope);
            }
            else if (mode == ParameterCaptureMode.Background)
            {
                if (!_messages.TryAdd(new QueuedLogStatement(methodTemplateString.Template, args, scope)))
                {
                    Interlocked.Increment(ref _droppedMessageCounter);
                }
            }
        }

        private static KeyValueLogScope GenerateScope(MethodTemplateString methodTemplateString)
        {
            KeyValueLogScope scope = new();

            // Store timestamp as ISO 8601 compliant
            scope.Values.Add(Scopes.TimeStamp, DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            scope.Values.Add(Scopes.ThreadId, Environment.CurrentManagedThreadId);

            scope.Values.Add(Scopes.CaptureSite.MethodName, methodTemplateString.MethodName);
            scope.Values.Add(Scopes.CaptureSite.ModuleName, methodTemplateString.ModuleName);
            scope.Values.Add(Scopes.CaptureSite.TypeName, methodTemplateString.TypeName);

            Activity? currentActivity = Activity.Current;
            if (currentActivity?.Id != null)
            {
                scope.Values.Add(Scopes.ActivityId, currentActivity.Id);
                scope.Values.Add(Scopes.ActivityIdFormat, currentActivity.IdFormat);
            }

            return scope;
        }

        private void ThreadProc()
        {
            InProcFeatureExecutionContextTracker.MarkInProcFeatureThread();

            try
            {
                while (_messages.TryTake(out QueuedLogStatement? entry, Timeout.InfiniteTimeSpan))
                {
                    Log(_systemLogger, entry.Format, entry.Args, entry.Scope);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch
            {
            }
        }

        public void Complete()
        {
            // NOTE We currently do not wait for the background thread in production code
            _messages.CompleteAdding();
            _thread.Join();
        }

        private static void Log(ILogger logger, string format, string[] args, KeyValueLogScope scope)
        {
            using var _ = logger.BeginScope(scope);
            logger.Log(LogLevel.Information, format, args);
        }

        public void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
            {
                return;
            }
            _messages.CompleteAdding();
            _messages.Dispose();
        }
    }
}
