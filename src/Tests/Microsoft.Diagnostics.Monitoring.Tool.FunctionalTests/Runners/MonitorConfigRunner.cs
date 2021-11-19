// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
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

        public string _configurationString = "";

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

            UserSettingsFilePath = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).FullName, "SampleConfigurations" , "Settings1.json");
            _useSettingsConfig = true;

            using IDisposable _ = token.Register(() => CancelCompletionSources(token));

            await base.StartAsync(command, argsList.ToArray(), token);

            Task<int> runnerExitTask = RunnerExitedTask;
            Task endingTask = await Task.WhenAny(_readySource.Task, runnerExitTask);
            // Await ready and exited tasks in case process exits before it is ready.
            if (runnerExitTask == endingTask)
            {
                //throw new InvalidOperationException("Process exited before it was ready.");
            }

            //await _readySource.Task;
        }

        protected override void StandardOutputCallback(string line)
        {
            _configurationString += line;
        }

        private void CancelCompletionSources(CancellationToken token)
        {
            _readySource.TrySetCanceled(token);
        }
    }
}
