// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.Monitoring.WebApi.Stacks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal sealed class ExceptionsStore : IExceptionsStore, IAsyncDisposable
    {
        private const int ChannelCapacity = 1000;

        private readonly Channel<ExceptionInstanceEntry> _channel;
        private readonly CancellationTokenSource _disposalSource = new();
        private readonly List<ExceptionInstance> _instances = new();
        private readonly Task _processingTask;

        private long _disposalState;

        public ExceptionsStore()
        {
            _channel = CreateChannel();
            _processingTask = ProcessEntriesAsync(_disposalSource.Token);
        }

        public async ValueTask DisposeAsync()
        {
            if (!DisposableHelper.CanDispose(ref _disposalState))
                return;

            _channel.Writer.TryComplete();

            await _processingTask.SafeAwait();

            _disposalSource.SafeCancel();

            _disposalSource.Dispose();
        }

        public void AddExceptionInstance(IExceptionsNameCache cache, ulong exceptionId, string message, DateTime timestamp)
        {
            ExceptionInstanceEntry entry = new(cache, exceptionId, message, timestamp);
            // This should never fail to write because the behavior is to drop the oldest.
            _channel.Writer.TryWrite(entry);
        }

        public IReadOnlyList<IExceptionInstance> GetSnapshot()
        {
            lock (_instances)
            {
                return new List<ExceptionInstance>(_instances).AsReadOnly();
            }
        }

        private static Channel<ExceptionInstanceEntry> CreateChannel()
        {
            // TODO: Hook callback for when items are dropped and report appropriately.
            return Channel.CreateBounded<ExceptionInstanceEntry>(
                new BoundedChannelOptions(ChannelCapacity)
                {
                    AllowSynchronousContinuations = false,
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true,
                    SingleWriter = true
                });
        }

        private async Task ProcessEntriesAsync(CancellationToken token)
        {
            StringBuilder _builder = new();
            Dictionary<ulong, string> _exceptionTypeNameMap = new();

            bool shouldReadEntry = await _channel.Reader.WaitToReadAsync(token);
            while (shouldReadEntry)
            {
                ExceptionInstanceEntry entry = await _channel.Reader.ReadAsync(token);

                // CONSIDER: If the exception ID could not be found, either the identification information was not sent
                // by the EventSource in the target application OR it hasn't been sent yet due to multithreaded collision
                // in the target application where the same exception information is being logged by two or more threads
                // at the same time; one will return sooner and report the correct IDs potentially before those IDs are
                // produced by the EventSource. May need to cache this incompletion information and attempt to reconstruct
                // it in the future, with either periodic retry OR registering a callback system for the missing IDs.
                if (entry.Cache.TryGetExceptionId(entry.ExceptionId, out ulong exceptionClassId, out _, out _))
                {
                    string exceptionTypeName;
                    if (!_exceptionTypeNameMap.TryGetValue(exceptionClassId, out exceptionTypeName))
                    {
                        _builder.Clear();
                        NameFormatter.BuildClassName(_builder, entry.Cache.NameCache, exceptionClassId);
                        exceptionTypeName = _builder.ToString();
                    }

                    string moduleName = string.Empty;
                    if (entry.Cache.NameCache.ClassData.TryGetValue(exceptionClassId, out ClassData exceptionClassData))
                    {
                        moduleName = NameFormatter.GetModuleName(entry.Cache.NameCache, exceptionClassData.ModuleId);
                    }

                    lock (_instances)
                    {
                        _instances.Add(new ExceptionInstance(exceptionTypeName, moduleName, entry.Message, entry.Timestamp));
                    }
                }

                shouldReadEntry = await _channel.Reader.WaitToReadAsync(token);
            }
        }

        private sealed class ExceptionInstanceEntry
        {
            public ExceptionInstanceEntry(IExceptionsNameCache cache, ulong exceptionId, string message, DateTime timestamp)
            {
                Cache = cache;
                ExceptionId = exceptionId;
                Message = message;
                Timestamp = timestamp;
            }

            public IExceptionsNameCache Cache { get; }

            public ulong ExceptionId { get; }

            public string Message { get; }

            public DateTime Timestamp { get; }
        }
    }
}
