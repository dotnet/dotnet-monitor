// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Tools.Monitor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners
{
    partial class MonitorCollectRunner
    {
        private readonly ConcurrentDictionary<CollectionRuleKey, List<TaskCompletionSource<object>>> _collectionRuleCallbacks = new();

        public async Task WaitForCollectionRuleCompleteAsync(string ruleName, CancellationToken token)
        {
            TaskCompletionSource<object> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

            CollectionRuleKey completedKey = new(LoggingEventIds.CollectionRuleCompleted, ruleName);
            CollectionRuleKey failedKey = new(LoggingEventIds.CollectionRuleFailed, ruleName);

            AddCollectionRuleCallback(completedKey, tcs);
            AddCollectionRuleCallback(failedKey, tcs);

            try
            {
                await tcs.WithCancellation(token);
            }
            finally
            {
                RemoveCollectionRuleCallback(completedKey, tcs);
                RemoveCollectionRuleCallback(failedKey, tcs);
            }
        }

        private void HandleCollectionRuleEvent(ConsoleLogEvent logEvent)
        {
            if (logEvent.State.TryGetValue("ruleName", out string ruleName))
            {
                CollectionRuleKey key = new(logEvent.EventId, ruleName);
                switch (logEvent.EventId)
                {
                    case LoggingEventIds.CollectionRuleFailed:
                        FailCollectionRuleCallbacks(key, logEvent.Exception);
                        break;
                    case LoggingEventIds.CollectionRuleCompleted:
                        CompleteCollectionRuleCallbacks(key);
                        break;
                }
            }
        }

        private List<TaskCompletionSource<object>> GetCollectionRuleCompletionSources(CollectionRuleKey key)
        {
            return _collectionRuleCallbacks.GetOrAdd(key, _ => new List<TaskCompletionSource<object>>());
        }

        private void AddCollectionRuleCallback(CollectionRuleKey key, TaskCompletionSource<object> completionSource)
        {
            List<TaskCompletionSource<object>> completionSources = GetCollectionRuleCompletionSources(key);
            lock (completionSources)
            {
                completionSources.Add(completionSource);
            }
        }

        private void RemoveCollectionRuleCallback(CollectionRuleKey key, TaskCompletionSource<object> completionSource)
        {
            List<TaskCompletionSource<object>> completionSources = GetCollectionRuleCompletionSources(key);
            lock (completionSources)
            {
                completionSources.Remove(completionSource);
            }
        }

        private void CompleteCollectionRuleCallbacks(CollectionRuleKey key)
        {
            List<TaskCompletionSource<object>> completionSources = GetCollectionRuleCompletionSources(key);
            lock (completionSources)
            {
                foreach (TaskCompletionSource<object> completionSource in completionSources)
                {
                    completionSource.TrySetResult(null);
                }
            }
        }

        private void FailCollectionRuleCallbacks(CollectionRuleKey key, string message)
        {
            List<TaskCompletionSource<object>> completionSources = GetCollectionRuleCompletionSources(key);
            InvalidOperationException ex = new InvalidOperationException(message);
            lock (completionSources)
            {
                foreach (TaskCompletionSource<object> completionSource in completionSources)
                {
                    completionSource.TrySetException(ex);
                }
            }
        }

        private struct CollectionRuleKey : IEquatable<CollectionRuleKey>
        {
            private readonly int _eventId;
            private readonly string _ruleName;

            public CollectionRuleKey(int eventId, string ruleName)
            {
                _eventId = eventId;
                _ruleName = ruleName;
            }

            public bool Equals(CollectionRuleKey other)
            {
                return _eventId == other._eventId &&
                    string.Equals(_ruleName, other._ruleName, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is CollectionRuleKey key && Equals(key);
            }

            public override int GetHashCode()
            {
                HashCode code = new();
                code.Add(_eventId);
                code.Add(_ruleName);
                return code.ToHashCode();
            }
        }
    }
}
