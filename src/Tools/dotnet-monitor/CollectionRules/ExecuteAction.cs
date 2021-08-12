using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules
{
    internal sealed class ExecuteAction : ICollectionRuleAction<ExecuteOptions>
    {
        // Completion source that is signaled when the process exits
        private TaskCompletionSource<int> _exitedSource;

        /// <summary>
        /// Gets a task that completes with the process exit code when it exits.
        /// </summary>
        public Task<int> ExitedTask => _exitedSource.Task;

        public async Task<ActionResult> ExecuteAsync(ExecuteOptions options, DiagnosticsClient client, CancellationToken cancellationToken)
        {
            string path = options.Path;
            string arguments = options.Arguments;

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("A file could not be found at the provided path: " + path);
            }

            // May want to capture stdout and stderr and return as part of the result in the future
            Process process = new Process();

            process.StartInfo = new ProcessStartInfo(path, arguments);
            process.EnableRaisingEvents = true;

            _exitedSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (!process.Start())
            {
                throw new InvalidOperationException($"Unable to start: {process.StartInfo.FileName} {process.StartInfo.Arguments}");
            }

            await WaitForExitAsync(process, cancellationToken).ConfigureAwait(false);

            int exitCode = process.ExitCode;

            if (!options.IgnoreExitCode && exitCode != 0)
            {
                // Do we want to use a specific type of Exception here? My thought was that the proper type of Exception may be dependent on the exitCode's value
                throw new Exception("The process exited with exit code " + exitCode.ToString());
            }

            ActionResult executeResponse = new();

            executeResponse.OutputValues = new Dictionary<string, string> { { "ExitCode", exitCode.ToString() } };

            return executeResponse;
        }

        /// <summary>
        /// Waits for the process to exit.
        /// </summary>
        public async Task WaitForExitAsync(Process process, CancellationToken token)
        {
            if (!process.HasExited)
            {
                TaskCompletionSource<object> cancellationSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
                using IDisposable _ = token.Register(() => cancellationSource.TrySetCanceled(token));

                await Task.WhenAny(
                    ExitedTask,
                    cancellationSource.Task).Unwrap();
            }
        }
    }
}