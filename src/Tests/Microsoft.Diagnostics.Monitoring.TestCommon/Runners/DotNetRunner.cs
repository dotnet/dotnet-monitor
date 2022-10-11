﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.TestCommon.Runners
{
    /// <summary>
    /// Runner for running dotnet processes.
    /// </summary>
    public sealed class DotNetRunner : IDisposable
    {
        // Event handler for the Process.Exited event
        private readonly EventHandler _exitedHandler;

        // Completion source that is signaled when the process exits
        private readonly TaskCompletionSource<int> _exitedSource;

        // The process object of the started process
        private readonly Process _process;

        /// <summary>
        /// The architecture of the dotnet host.
        /// </summary>
        public Architecture? Architecture { get; set; } = null;

        /// <summary>
        /// The arguments to the entrypoint method.
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// The path of the assembly containing the entrypoint method.
        /// </summary>
        public string EntrypointAssemblyPath { get; set; }

        /// <summary>
        /// Retrives the starting environment block of the process.
        /// </summary>
        public IDictionary<string, string> Environment => _process.StartInfo.Environment;

        /// <summary>
        /// Gets a <see cref="bool"/> indicating if <see cref="StartAsync(CancellationToken)"/> has been called and the process has been started.
        /// </summary>
        public bool HasStarted { get; private set; } = false;

        /// <summary>
        /// Retrieves the exit code of the process.
        /// </summary>
        public int ExitCode => _process.ExitCode;

        /// <summary>
        /// Gets a task that completes with the process exit code when it exits.
        /// </summary>
        public Task<int> ExitedTask => _exitedSource.Task;

        /// <summary>
        /// Determines if the process has exited.
        /// </summary>
        public bool HasExited => HasStarted && _process.HasExited;

        /// <summary>
        /// Gets the process ID of the running process.
        /// </summary>
        public int ProcessId => _process.Id;

        /// <summary>
        /// Gets a <see cref="StreamReader"/> that reads stderr.
        /// </summary>
        public StreamReader StandardError => _process.StandardError;

        /// <summary>
        /// Gets a <see cref="StreamWriter"/> that writes to stdin.
        /// </summary>
        public StreamWriter StandardInput => _process.StandardInput;

        /// <summary>
        /// Gets a <see cref="StreamReader"/> that reads stdout.
        /// </summary>
        public StreamReader StandardOutput => _process.StandardOutput;

        /// <summary>
        /// Determines if <see cref="StartAsync(CancellationToken)" /> should wait for the diagnostic pipe to be available.
        /// </summary>
        public bool WaitForDiagnosticPipe { get; set; }

        public DotNetRunner()
        {
            _process = new Process();
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.RedirectStandardInput = true;
            _process.StartInfo.RedirectStandardOutput = true;

            _exitedSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            _exitedHandler = (s, e) => _exitedSource.SetResult(_process.ExitCode);

            _process.EnableRaisingEvents = true;
            _process.Exited += _exitedHandler;
        }

        public void Dispose()
        {
            ForceClose();

            _process.Dispose();
        }

        /// <summary>
        /// Starts the dotnet process.
        /// </summary>
        public async Task StartAsync(CancellationToken token)
        {
            StringBuilder argsBuilder = new();
            if (DotNetHost.HasHostInRepository)
            {
                argsBuilder.Append("exec --runtimeconfig \"");
                argsBuilder.Append(Path.ChangeExtension(EntrypointAssemblyPath, ".runtimeconfig.test.json"));
                argsBuilder.Append("\" ");
            }
            argsBuilder.Append("\"");
            argsBuilder.Append(EntrypointAssemblyPath);
            argsBuilder.Append("\" ");
            argsBuilder.Append(Arguments);

            _process.StartInfo.FileName = DotNetHost.GetPath(Architecture);
            _process.StartInfo.Arguments = argsBuilder.ToString();

            if (!_process.Start())
            {
                throw new InvalidOperationException($"Unable to start: {_process.StartInfo.FileName} {_process.StartInfo.Arguments}");
            }
            HasStarted = true;

            if (WaitForDiagnosticPipe)
            {
                // On Windows, named pipe connection will block until the named pipe is ready to connect so no need to block here
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // On Unix, we wait until the socket is created.
                    while (true)
                    {
                        token.ThrowIfCancellationRequested();

                        var matchingFiles = Directory.GetFiles(Path.GetTempPath(), $"dotnet-diagnostic-{_process.Id}-*-socket");
                        if (matchingFiles.Length > 0)
                        {
                            break;
                        }

                        await Task.Delay(TimeSpan.FromMilliseconds(100));
                    }
                }
            }
        }

        /// <summary>
        /// Waits for the process to exit.
        /// </summary>
        public async Task WaitForExitAsync(CancellationToken token)
        {
            if (HasStarted && !_process.HasExited)
            {
                await ExitedTask.WithCancellation(token);
            }
        }

        /// <summary>
        /// Forces the process to exit.
        /// </summary>
        public void ForceClose()
        {
            if (HasStarted && !_process.HasExited)
            {
                _process.Kill();
            }
        }
    }
}
