using Microsoft.Diagnostics.Monitoring.EventPipe;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class PipelineExtensions
    {
        public static async Task<Task> StartAsync<T>(this EventSourcePipeline<T> pipeline, CancellationToken token)
            where T : EventSourcePipelineSettings
        {
            Task runTask = pipeline.RunAsync(token);

            IEventSourcePipelineInternal pipelineInternal = pipeline;

            // Await both the session started or the run task and return when either is completed.
            // This works around an issue where the run task may fail but not cancel/fault the session
            // started task. Logically, the run task will not successfully complete before the session
            // started task. Thus, the combined task completes either when the session started task is
            // completed OR the run task has failed.
            await Task.WhenAny(pipelineInternal.SessionStarted, runTask);

            return runTask;
        }
    }
}
