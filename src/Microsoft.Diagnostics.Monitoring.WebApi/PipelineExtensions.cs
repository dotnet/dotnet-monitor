// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Monitoring.EventPipe;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class PipelineExtensions
    {
        public static async Task<Task> StartAsync<T>(this EventSourcePipeline<T> pipeline, CancellationToken token)
            where T : EventSourcePipelineSettings
        {
            Task runTask = pipeline.RunAsync(token);

            // Await both the session started or the run task and return when either is completed.
            // This works around an issue where the run task may fail but not cancel/fault the session
            // started task. Logically, the run task will not successfully complete before the session
            // started task. Thus, the combined task completes either when the session started task is
            // completed OR the run task has cancelled/failed.
            IEventSourcePipelineInternal pipelineInternal = pipeline;
            await Task.WhenAny(pipelineInternal.SessionStarted, runTask).Unwrap();

            return runTask;
        }
    }
}
