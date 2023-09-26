// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
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
        private readonly ConcurrentDictionary<int, List<TaskCompletionSource<object>>> _eventCallbacks = new();

        public Task WaitForCollectionRuleActionsCompletedAsync(string ruleName, CancellationToken token)
        {
            return WaitForCollectionRuleEventAsync(LoggingEventIds.CollectionRuleActionsCompleted.Id(), ruleName, token);
        }

        public Task WaitForCollectionRuleCompleteAsync(string ruleName, CancellationToken token)
        {
            return WaitForCollectionRuleEventAsync(LoggingEventIds.CollectionRuleCompleted.Id(), ruleName, token);
        }

        public Task WaitForCollectionRuleUnmatchedFiltersAsync(string ruleName, CancellationToken token)
        {
            return WaitForCollectionRuleEventAsync(LoggingEventIds.CollectionRuleUnmatchedFilters.Id(), ruleName, token);
        }

        public Task WaitForCollectionRuleStartedAsync(string ruleName, CancellationToken token)
        {
            return WaitForCollectionRuleEventAsync(LoggingEventIds.CollectionRuleStarted.Id(), ruleName, token);
        }

        public Task WaitForCollectionRulesStoppedAsync(CancellationToken token)
        {
            return WaitForEventAsync(LoggingEventIds.CollectionRulesStopped.Id(), token);
        }

        private async Task WaitForCollectionRuleEventAsync(int eventId, string ruleName, CancellationToken token)
        {
            TaskCompletionSource<object> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

            CollectionRuleKey eventKey = new(eventId, ruleName);
            CollectionRuleKey failedKey = new(LoggingEventIds.CollectionRuleFailed.Id(), ruleName);

            AddCollectionRuleCallback(eventKey, tcs);
            AddCollectionRuleCallback(failedKey, tcs);

            try
            {
                await tcs.WithCancellation(token);
            }
            finally
            {
                RemoveCollectionRuleCallback(eventKey, tcs);
                RemoveCollectionRuleCallback(failedKey, tcs);
            }
        }

        private async Task WaitForEventAsync(int eventId, CancellationToken token)
        {
            TaskCompletionSource<object> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

            AddEventCallback(eventId, tcs);

            try
            {
                await tcs.WithCancellation(token);
            }
            finally
            {
                RemoveEventCallback(eventId, tcs);
            }
        }

        private void HandleCollectionRuleEvent(ConsoleLogEvent logEvent)
        {
            if (logEvent.State.TryGetValue("ruleName", out string ruleName))
            {
                CollectionRuleKey key = new(logEvent.EventId, ruleName);
                switch ((LoggingEventIds)logEvent.EventId)
                {
                    case LoggingEventIds.CollectionRuleActionsCompleted:
                    case LoggingEventIds.CollectionRuleCompleted:
                    case LoggingEventIds.CollectionRuleUnmatchedFilters:
                    case LoggingEventIds.CollectionRuleStarted:
                        CompleteCollectionRuleCallbacks(key);
                        break;
                    case LoggingEventIds.CollectionRuleFailed:
                        FailCollectionRuleCallbacks(key, logEvent.Exception);
                        break;
                }
            }
            else
            {
                switch ((LoggingEventIds)logEvent.EventId)
                {
                    case LoggingEventIds.CollectionRulesStopped:
                        CompleteEventCallbacks(logEvent.EventId);
                        break;
                }
            }
        }

        private List<TaskCompletionSource<object>> GetCollectionRuleCompletionSources(CollectionRuleKey key)
        {
            return _collectionRuleCallbacks.GetOrAdd(key, _ => new List<TaskCompletionSource<object>>());
        }

        private List<TaskCompletionSource<object>> GetEventCompletionSources(int eventId)
        {
            return _eventCallbacks.GetOrAdd(eventId, _ => new List<TaskCompletionSource<object>>());
        }

        private void AddCollectionRuleCallback(CollectionRuleKey key, TaskCompletionSource<object> completionSource)
        {
            List<TaskCompletionSource<object>> completionSources = GetCollectionRuleCompletionSources(key);
            lock (completionSources)
            {
                completionSources.Add(completionSource);
            }
        }

        private void AddEventCallback(int eventId, TaskCompletionSource<object> completionSource)
        {
            List<TaskCompletionSource<object>> completionSources = GetEventCompletionSources(eventId);
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

        private void RemoveEventCallback(int eventId, TaskCompletionSource<object> completionSource)
        {
            List<TaskCompletionSource<object>> completionSources = GetEventCompletionSources(eventId);
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

        private void CompleteEventCallbacks(int eventId)
        {
            List<TaskCompletionSource<object>> completionSources = GetEventCompletionSources(eventId);
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

        private record struct CollectionRuleKey(int EventId, string RuleName);
    }
}
