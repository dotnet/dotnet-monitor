// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
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
        public ICollectionRuleAction Create(IProcessInfo processInfo, ExecuteOptions options)
        {
            if (null == options)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ValidationContext context = new(options, null, items: null);
            Validator.ValidateObject(options, context, validateAllProperties: true);

            return new ExecuteAction(processInfo, options);
        }

        internal sealed class ExecuteAction :
            CollectionRuleActionBase<ExecuteOptions>
        {
            public ExecuteAction(IProcessInfo processInfo, ExecuteOptions options)
                : base(processInfo, options)
            {
            }

            protected override async Task<CollectionRuleActionResult> ExecuteCoreAsync(
                CollectionRuleMetadata? collectionRuleMetadata,
                CancellationToken token)
            {
                string path = Options.Path;
                string? arguments = Options.Arguments;
                bool IgnoreExitCode = Options.IgnoreExitCode.GetValueOrDefault(ExecuteOptionsDefaults.IgnoreExitCode);

                ValidateFilePath(path);

                // May want to capture stdout and stderr and return as part of the result in the future
                using Process process = new Process();

                process.StartInfo = new ProcessStartInfo(path, arguments ?? string.Empty);
                process.StartInfo.RedirectStandardOutput = true;

                process.EnableRaisingEvents = true;

                // Signaled when process exits
                using EventTaskSource<EventHandler> exitedSource = new(
                    complete => (s, e) => complete(),
                    handler => process.Exited += handler,
                    handler => process.Exited -= handler,
                    token);

                if (!process.Start())
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Strings.ErrorMessage_UnableToStartProcess, process.StartInfo.FileName, process.StartInfo.Arguments));
                }

                if (!TrySetStarted())
                {
                    throw new InvalidOperationException();
                }

                // Wait for process to exit; cancellation is handled by the exitedSource
                await exitedSource.Task.ConfigureAwait(false);

                ValidateExitCode(IgnoreExitCode, process.ExitCode);

                return new CollectionRuleActionResult()
                {
                    OutputValues = new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            { "ExitCode", process.ExitCode.ToString(CultureInfo.InvariantCulture) }
                        }
                };
            }

            internal static void ValidateFilePath(string path)
            {
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException(string.Format(CultureInfo.InvariantCulture, Strings.ErrorMessage_FileNotFound, path));
                }
            }

            internal static void ValidateExitCode(bool ignoreExitCode, int exitCode)
            {
                if (!ignoreExitCode && exitCode != 0)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Strings.ErrorMessage_NonzeroExitCode, exitCode.ToString(CultureInfo.InvariantCulture)));
                }
            }
        }
    }
}
