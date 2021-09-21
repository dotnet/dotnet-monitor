// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class ExecuteActionFactory :
        ICollectionRuleActionFactory<ExecuteOptions>
    {
        public ICollectionRuleAction Create(IEndpointInfo endpointInfo, ExecuteOptions options)
        {
            if (null == options)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ValidationContext context = new(options, null, items: null);
            Validator.ValidateObject(options, context, validateAllProperties: true);

            return new ExecuteAction(options);
        }

        internal sealed class ExecuteAction :
            ICollectionRuleAction,
            IAsyncDisposable
        {
            private readonly CancellationTokenSource _disposalTokenSource = new();
            private readonly ExecuteOptions _options;

            private Task<CollectionRuleActionResult> _completionTask;

            public ExecuteAction(ExecuteOptions options)
            {
                _options = options ?? throw new ArgumentNullException(nameof(options));
            }

            public async ValueTask DisposeAsync()
            {
                _disposalTokenSource.SafeCancel();

                await _completionTask.SafeAwait();

                _disposalTokenSource.Dispose();
            }

            public async Task StartAsync(CancellationToken token)
            {
                TaskCompletionSource<object> startCompleteSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

                CancellationToken disposalToken = _disposalTokenSource.Token;
                _completionTask = Task.Run(() => ExecuteAsync(startCompleteSource, disposalToken), disposalToken);

                await startCompleteSource.WithCancellation(token);
            }

            public Task<CollectionRuleActionResult> WaitForCompletionAsync(CancellationToken token)
            {
                return _completionTask.WithCancellation(token);
            }

            private async Task<CollectionRuleActionResult> ExecuteAsync(TaskCompletionSource<object> startCompleteSource, CancellationToken token)
            {
                try
                {
                    string path = _options.Path;
                    string arguments = _options.Arguments;
                    bool IgnoreExitCode = _options.IgnoreExitCode.GetValueOrDefault(ExecuteOptionsDefaults.IgnoreExitCode);

                    ValidateFilePath(path);

                    // May want to capture stdout and stderr and return as part of the result in the future
                    using Process process = new Process();

                    process.StartInfo = new ProcessStartInfo(path, arguments);
                    process.StartInfo.RedirectStandardOutput = true;

                    process.EnableRaisingEvents = true;

                    // Signaled when process exits
                    using EventTaskSource<EventHandler> exitedSource = new(
                        complete => (s, e) => complete(),
                        handler => process.Exited += handler,
                        handler => process.Exited -= handler);

                    if (!process.Start())
                    {
                        throw new CollectionRuleActionException(new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Strings.ErrorMessage_UnableToStartProcess, process.StartInfo.FileName, process.StartInfo.Arguments)));
                    }

                    startCompleteSource.TrySetResult(null);

                    await WaitForExitAsync(process, exitedSource.Task, token).ConfigureAwait(false);

                    ValidateExitCode(IgnoreExitCode, process.ExitCode);

                    return new CollectionRuleActionResult()
                    {
                        OutputValues = new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            { "ExitCode", process.ExitCode.ToString(CultureInfo.InvariantCulture) }
                        }
                    };
                }
                catch (Exception ex) when (TrySetExceptionReturnFalse(startCompleteSource, ex))
                {
                    throw;
                }
            }

            private static bool TrySetExceptionReturnFalse(TaskCompletionSource<object> source, Exception ex)
            {
                source.TrySetException(ex);
                return false;
            }

            public async Task WaitForExitAsync(Process process, Task exitedTask, CancellationToken token)
            {
                if (!process.HasExited)
                {
                    await exitedTask.WithCancellation(token).ConfigureAwait(false);
                }
            }

            internal static void ValidateFilePath(string path)
            {
                if (!File.Exists(path))
                {
                    throw new CollectionRuleActionException(new FileNotFoundException(string.Format(CultureInfo.InvariantCulture, Strings.ErrorMessage_FileNotFound, path)));
                }
            }

            internal static void ValidateExitCode(bool ignoreExitCode, int exitCode)
            {
                if (!ignoreExitCode && exitCode != 0)
                {
                    throw new CollectionRuleActionException(new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Strings.ErrorMessage_NonzeroExitCode, exitCode.ToString(CultureInfo.InvariantCulture))));
                }
            }
        }
    }
}