// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal sealed class ConfiguredExceptionsStore :
        IExceptionsStore,
        IAsyncDisposable
    {
        private readonly ExceptionsStore _store;

        public ConfiguredExceptionsStore(IOptions<ExceptionsOptions> options)
            : this(options.Value.GetTopLevelLimit(), callback: null)
        {
        }

        internal ConfiguredExceptionsStore(int topLevelLimit, ExceptionsStoreCallback callback = null)
        {
            if (topLevelLimit <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(topLevelLimit));
            }

            _store = new ExceptionsStore(new Callback(this, topLevelLimit, callback));
        }

        public ValueTask DisposeAsync()
        {
            return _store.DisposeAsync();
        }

        public void AddExceptionInstance(IExceptionsNameCache cache, ulong exceptionId, ulong groupId, string message, DateTime timestamp, ulong[] stackFrameIds, int threadId, ulong[] innerExceptionIds, string activityId, ActivityIdFormat activityIdFormat)
        {
            _store.AddExceptionInstance(cache, exceptionId, groupId, message, timestamp, stackFrameIds, threadId, innerExceptionIds, activityId, activityIdFormat);
        }

        public void RemoveExceptionInstance(ulong exceptionId)
        {
            _store.RemoveExceptionInstance(exceptionId);
        }

        public IReadOnlyList<IExceptionInstance> GetSnapshot()
        {
            return _store.GetSnapshot();
        }

        private sealed class Callback : ExceptionsStoreCallback
        {
            private readonly ExceptionsStoreCallback _callback;
            private readonly Dictionary<ulong, List<ulong>> _innerExceptionMap = new();
            private readonly Dictionary<ulong, List<ulong>> _outerExceptionMap = new();
            private readonly ConfiguredExceptionsStore _store;
            private readonly LinkedList<ulong> _topLevelExceptions = new();
            private readonly int _topLevelLimit;

            public Callback(ConfiguredExceptionsStore store, int topLevelLimit, ExceptionsStoreCallback callback)
            {
                _callback = callback;
                _store = store;
                _topLevelLimit = topLevelLimit;
            }

            public override void AfterAdd(IExceptionInstance instance)
            {
                _callback?.AfterAdd(instance);
            }

            public override void BeforeAdd(IExceptionInstance instance)
            {
                _outerExceptionMap.Add(instance.Id, new List<ulong>());
                _innerExceptionMap.Add(instance.Id, instance.InnerExceptionIds.ToList());

                // Make sure all of the inner exceptions are no longer top-level exceptions.
                foreach (ulong innerExceptionId in instance.InnerExceptionIds)
                {
                    if (_outerExceptionMap.TryGetValue(innerExceptionId, out List<ulong> outerExceptions))
                    {
                        outerExceptions.Add(instance.Id);
                    }

                    _topLevelExceptions.Remove(innerExceptionId);
                }

                _topLevelExceptions.AddLast(instance.Id);

                // If over the limit, remove the oldest exception as well as its inner
                // exceptions that are not shared with any current top-level exception.
                if (_topLevelExceptions.Count > _topLevelLimit)
                {
                    LinkedListNode<ulong> firstNode = _topLevelExceptions.First;
                    _topLevelExceptions.Remove(firstNode);

                    RemoveIfNoOuterExceptions(firstNode.Value);
                }

                _callback?.BeforeAdd(instance);
            }

            private void RemoveIfNoOuterExceptions(ulong exceptionId)
            {
                // Remove the exception if it no longer belongs to any other exception in the store
                // as an inner exception.
                if (_outerExceptionMap.TryGetValue(exceptionId, out List<ulong> outerExceptionIds))
                {
                    if (outerExceptionIds.Count == 0)
                    {
                        _outerExceptionMap.Remove(exceptionId);
                        _store.RemoveExceptionInstance(exceptionId);

                        // Check the inner exceptions of this exception, decouple them from this exception,
                        // and remove them if they are not shared with any current top-level exception.
                        if (_innerExceptionMap.Remove(exceptionId, out List<ulong> innerExceptionIds))
                        {
                            foreach (ulong innerExceptionId in innerExceptionIds)
                            {
                                if (_outerExceptionMap.TryGetValue(innerExceptionId, out List<ulong> innerOuterExceptionIds))
                                {
                                    innerOuterExceptionIds.Remove(exceptionId);
                                }

                                RemoveIfNoOuterExceptions(innerExceptionId);
                            }
                        }
                    }
                }
            }
        }
    }
}
