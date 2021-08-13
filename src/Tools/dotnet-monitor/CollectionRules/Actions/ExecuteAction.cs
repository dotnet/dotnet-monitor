// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class ExecuteAction : ICollectionRuleAction<ExecuteOptions>
    {
        public async Task<CollectionRuleActionResult> ExecuteAsync(ExecuteOptions options, DiagnosticsClient client, CancellationToken cancellationToken)
        {
            string path = options.Path;
            string arguments = options.Arguments;
            bool IgnoreExitCode = options.IgnoreExitCode.GetValueOrDefault(ExecuteOptionsDefaults.IgnoreExitCode);

            ValidateFilePathValidity(path);

            // May want to capture stdout and stderr and return as part of the result in the future
            Process process = new Process();

            process.StartInfo = new ProcessStartInfo(path, arguments);
            process.EnableRaisingEvents = true;

            // Completion source that is signaled when the process exits
            TaskCompletionSource<int> exitedSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            EventHandler exitedHandler = (s, e) => exitedSource.SetResult(process.ExitCode);

            process.EnableRaisingEvents = true;
            process.Exited += exitedHandler;

            try
            {
                if (!process.Start())
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_UnableToStartProcess, process.StartInfo.FileName, process.StartInfo.Arguments));
                }

                await WaitForExitAsync(process, exitedSource.Task, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ValidateExitCode(IgnoreExitCode, process.ExitCode);
            }

            return new CollectionRuleActionResult()
            {
                OutputValues = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "ExitCode", process.ExitCode.ToString(CultureInfo.InvariantCulture) }
                }
            };
        }

        public async Task WaitForExitAsync(Process process, Task<int> exitedTask, CancellationToken token)
        {
            if (!process.HasExited)
            {
                await Task.WhenAny(exitedTask, Task.Delay(Timeout.Infinite, token)).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
            }
        }

        internal static void ValidateFilePathValidity(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_FileNotFound, path));
            }
        }

        internal static void ValidateExitCode(bool ignoreExitCode, int exitCode)
        {
            if (!ignoreExitCode && exitCode != 0)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_NonzeroExitCode, exitCode.ToString(CultureInfo.InvariantCulture)));
            }
        }
    }
}