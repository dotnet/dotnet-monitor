// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal sealed class ExceptionsStoreLimitsCallback : ExceptionsStoreCallbackBase
    {
        // Note: The term "outer exception" refers to an exception at the other end of the "parent" relationship
        // of a given exception; it is possible that a given exception has many outer exceptions, but it is
        // fairly unlikely for there to be more than one outer exception for a given exception.
        // Example: if exception A has an inner exception B, then A is an outer exception of B.

        // Track the list of inner exceptions for a given exception; allows for quicker
        // access to this relationship without having to look up IExceptionInstances.
        private readonly Dictionary<ulong, List<ulong>> _innerExceptionMap = new();
        // Track the list of outer exceptions for a given exception; allows for quicker
        // access to this relationship without having to look up IExceptionInstances.
        private readonly Dictionary<ulong, List<ulong>> _outerExceptionMap = new();
        private readonly IExceptionsStore _store;
        // Front of the list has the latest top-level exception ID and the
        // back of the list has the oldest top-level exception ID.
        private readonly LinkedList<ulong> _topLevelExceptions = new();
        private readonly int _topLevelLimit;

        public ExceptionsStoreLimitsCallback(IExceptionsStore store, int topLevelLimit)
        {
            ArgumentNullException.ThrowIfNull(store);
#if NET8_0_OR_GREATER
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(topLevelLimit, 0);
#else
            if (topLevelLimit <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(topLevelLimit));
            }
#endif

            _store = store;
            _topLevelLimit = topLevelLimit;
        }

        public override void BeforeAdd(IExceptionInstance instance)
        {
            _outerExceptionMap.Add(instance.Id, new List<ulong>());
            _innerExceptionMap.Add(instance.Id, instance.InnerExceptionIds.ToList());

            // Make sure all of the inner exceptions are no longer top-level exceptions.
            foreach (ulong innerExceptionId in instance.InnerExceptionIds)
            {
                if (_outerExceptionMap.TryGetValue(innerExceptionId, out List<ulong>? outerExceptions))
                {
                    outerExceptions.Add(instance.Id);
                }

                _topLevelExceptions.Remove(innerExceptionId);
            }

            _topLevelExceptions.AddFirst(instance.Id);

            // If over the limit, remove the oldest exception as well as its inner
            // exceptions that are not shared with any current top-level exception.
            if (_topLevelExceptions.Count > _topLevelLimit)
            {
                // The linked list is guaranteed to have an entry due to the above check and constraints on _topLevelLimit
                LinkedListNode<ulong> lastNode = _topLevelExceptions.Last!;
                _topLevelExceptions.RemoveLast();
                RemoveIfNoOuterExceptions(lastNode.Value);
            }
        }

        private void RemoveIfNoOuterExceptions(ulong exceptionId)
        {
            // Remove the exception if it no longer belongs to any other exception in the store
            // as an inner exception.
            if (_outerExceptionMap.TryGetValue(exceptionId, out List<ulong>? outerExceptionIds))
            {
                if (outerExceptionIds.Count == 0)
                {
                    _outerExceptionMap.Remove(exceptionId);
                    _store.RemoveExceptionInstance(exceptionId);

                    // Check the inner exceptions of this exception, decouple them from this exception,
                    // and remove them if they are not shared with any current top-level exception.
                    if (_innerExceptionMap.Remove(exceptionId, out List<ulong>? innerExceptionIds))
                    {
                        foreach (ulong innerExceptionId in innerExceptionIds)
                        {
                            if (_outerExceptionMap.TryGetValue(innerExceptionId, out List<ulong>? innerOuterExceptionIds))
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
