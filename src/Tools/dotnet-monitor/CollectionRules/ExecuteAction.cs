using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules
{
    internal sealed class ExecuteAction : IAction<ExecuteOptions>
    {
        public async Task<ActionResponse> Execute(ExecuteOptions options, CancellationToken token)
        {
            ActionResponse executeResponse = new();

            await Task.Run(() =>
            {
                string path = options.Path;
                string arguments = options.Arguments;

                Process process = Process.Start(path, arguments);

                process.WaitForExit();

                int exitCode = process.ExitCode;

                executeResponse.OutputValues = new Dictionary<string, string> { { "ExitCode", exitCode.ToString() } };

            }, token);

            return executeResponse;
        }
    }
}