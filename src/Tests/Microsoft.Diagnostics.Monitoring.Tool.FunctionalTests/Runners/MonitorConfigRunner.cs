// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners
{
    /// <summary>
    /// Runner for the dotnet-monitor tool.
    /// </summary>
    internal sealed partial class MonitorConfigRunner : MonitorRunner
    {
        // Completion source containing a string which is fired when the monitor enters a ready idle state
        private readonly TaskCompletionSource<string> _readySource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private bool _isDisposed;

        public string ConfigurationString { get; set; }

        public string UserFileName { get; set; }

        /// <summary>
        /// Determines whether or not certain information is redacted from the displayed configuration.
        /// </summary>
        public bool Redact { get; set; }

        public MonitorConfigRunner(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        public override async ValueTask DisposeAsync()
        {
            lock (_lock)
            {
                if (_isDisposed)
                {
                    return;
                }
                _isDisposed = true;
            }

            CancelCompletionSources(CancellationToken.None);

            await base.DisposeAsync();
        }

        public async Task StartAsync(CancellationToken token)
        {
            List<string> argsList = new();

            const string command = "config show";

            if (Redact)
            {
                argsList.Add("--level redacted");
            }
            else
            {
                argsList.Add("--level full");
            }

            UserSettingsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "SampleConfigurations" , UserFileName);

            using IDisposable _ = token.Register(() => CancelCompletionSources(token));

            await base.StartAsync(command, argsList.ToArray(), token);

            Task<int> runnerExitTask = RunnerExitedTask;
            await Task.WhenAny(_readySource.Task, runnerExitTask);
        }

        protected override void StandardOutputCallback(string line)
        {
            ConfigurationString += line;
        }

        private void CancelCompletionSources(CancellationToken token)
        {
            _readySource.TrySetCanceled(token);
        }
    }
}
