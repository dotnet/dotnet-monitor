// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners
{
    partial class MonitorCollectRunner
    {
        private readonly ConcurrentDictionary<ArtifactEventKey, List<TaskCompletionSource<object>>> _artifactCallbacks = new();

        public Task WaitForStartCollectArtifactAsync(string artifactType, CancellationToken token)
        {
            return WaitForArtifactEventAsync(LoggingEventIds.StartCollectArtifact.Id(), artifactType, token);
        }

        private async Task WaitForArtifactEventAsync(int eventId, string artifactType, CancellationToken token)
        {
            TaskCompletionSource<object> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

            ArtifactEventKey eventKey = new(eventId, artifactType);

            AddArtifactEventCallback(eventKey, tcs);

            try
            {
                await tcs.WithCancellation(token);
            }
            finally
            {
                RemoveArtifactEventCallback(eventKey, tcs);
            }
        }

        private void HandleArtifactEvent(ConsoleLogEvent logEvent)
        {
            if (logEvent.State.TryGetValue("artifactType", out string artifactType))
            {
                ArtifactEventKey key = new(logEvent.EventId, artifactType);
                switch ((LoggingEventIds)logEvent.EventId)
                {
                    case LoggingEventIds.StartCollectArtifact:
                        CompleteArtifactEventCallbacks(key);
                        break;
                }
            }
        }

        private List<TaskCompletionSource<object>> GetArtifactEventCompletionSources(ArtifactEventKey key)
        {
            return _artifactCallbacks.GetOrAdd(key, _ => new List<TaskCompletionSource<object>>());
        }

        private void AddArtifactEventCallback(ArtifactEventKey key, TaskCompletionSource<object> completionSource)
        {
            List<TaskCompletionSource<object>> completionSources = GetArtifactEventCompletionSources(key);
            lock (completionSources)
            {
                completionSources.Add(completionSource);
            }
        }

        private void RemoveArtifactEventCallback(ArtifactEventKey key, TaskCompletionSource<object> completionSource)
        {
            List<TaskCompletionSource<object>> completionSources = GetArtifactEventCompletionSources(key);
            lock (completionSources)
            {
                completionSources.Remove(completionSource);
            }
        }

        private void CompleteArtifactEventCallbacks(ArtifactEventKey key)
        {
            List<TaskCompletionSource<object>> completionSources = GetArtifactEventCompletionSources(key);
            lock (completionSources)
            {
                foreach (TaskCompletionSource<object> completionSource in completionSources)
                {
                    completionSource.TrySetResult(null);
                }
            }
        }

        private record struct ArtifactEventKey(int EventId, string ArtifactType);
    }
}
