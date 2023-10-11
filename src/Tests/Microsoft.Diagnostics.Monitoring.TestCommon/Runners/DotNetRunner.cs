// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.TestCommon.Runners
{
    /// <summary>
    /// Runner for running dotnet processes.
    /// </summary>
    [DebuggerDisplay(@"\{DotNetRunner:{StateForDebuggerDisplay,nq}\}")]
    public sealed class DotNetRunner : IDisposable
    {
        private string StateForDebuggerDisplay =>
            !HasStarted ? "Not started" :
            HasExited ? $"Exited with code: {ExitCode}" :
            FormattableString.Invariant($"ProcessId={ProcessId}");

        private const string TestProcessCleanupStartupHookAssemblyName = "Microsoft.Diagnostics.Monitoring.TestProcessCleanupStartupHook";

        // Event handler for the Process.Exited event
        private readonly EventHandler _exitedHandler;

        // Completion source that is signaled when the process exits
        private readonly TaskCompletionSource<int> _exitedSource;

        // The process object of the started process
        private readonly Process _process;

        private long _disposedState;

        /// <summary>
        /// The architecture of the dotnet host.
        /// </summary>
        public Architecture? Architecture { get; set; }

        /// <summary>
        /// The arguments to the entrypoint method.
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// The path of the assembly containing the entrypoint method.
        /// </summary>
        public string EntrypointAssemblyPath { get; set; }

        /// <summary>
        /// Retrieves the starting environment block of the process.
        /// </summary>
        public IDictionary<string, string> Environment => _process.StartInfo.Environment;

        /// <summary>
        /// Gets a <see cref="bool"/> indicating if <see cref="StartAsync(CancellationToken)"/> has been called and the process has been started.
        /// </summary>
        public bool HasStarted { get; private set; }

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

        /// <summary>
        /// Determines if the spawned process should be stopped when the currently executing process exits.
        /// </summary>
        public bool StopOnParentExit { get; set; } = true;

        // This startup hook assembly is a dependency of the TestCommon assembly, so it is copied to the same
        // output directory that TestCommon is copied. Do not specify TFM so it uses the one that is in the
        // same directory; this is interpreted as "relative to the currently executing assembly,
        // find the startup hook assembly."
        private static string TestProcessCleanupStartupHookPath =>
            AssemblyHelper.GetAssemblyArtifactBinPath(
                Assembly.GetExecutingAssembly(),
                TestProcessCleanupStartupHookAssemblyName
                );

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
            if (!DisposableHelper.CanDispose(ref _disposedState))
            {
                return;
            }

            Stop();

            _process.Dispose();
        }

        /// <summary>
        /// Starts the dotnet process.
        /// </summary>
        public async Task StartAsync(CancellationToken token)
        {
            StringBuilder argsBuilder = new();
            if (TestDotNetHost.HasHostFullPath)
            {
                argsBuilder.Append("exec --runtimeconfig \"");
                argsBuilder.Append(Path.ChangeExtension(EntrypointAssemblyPath, ".runtimeconfig.test.json"));
                argsBuilder.Append("\" ");
            }
            argsBuilder.Append('\"');
            argsBuilder.Append(EntrypointAssemblyPath);
            argsBuilder.Append("\" ");
            argsBuilder.Append(Arguments);

            if (StopOnParentExit)
            {
                int pid;
#if NET5_0_OR_GREATER
                pid = System.Environment.ProcessId;
#else
                using (Process process = Process.GetCurrentProcess())
                {
                    pid = process.Id;
                }
#endif
                Environment.Add(TestProcessCleanupIdentifiers.EnvironmentVariables.ParentPid, pid.ToString(CultureInfo.InvariantCulture));

                if (Environment.TryGetValue(ToolIdentifiers.EnvironmentVariables.StartupHooks, out string startupHooks) &&
                    !string.IsNullOrEmpty(startupHooks))
                {
                    startupHooks += Path.PathSeparator + TestProcessCleanupStartupHookPath;
                }
                else
                {
                    startupHooks = TestProcessCleanupStartupHookPath;
                }

                Environment.Add(ToolIdentifiers.EnvironmentVariables.StartupHooks, startupHooks);
            }

            _process.StartInfo.FileName = TestDotNetHost.GetPath(Architecture);
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

                        await Task.Delay(TimeSpan.FromMilliseconds(100), token);
                    }
                }
            }
        }

        /// <summary>
        /// Forces the process to stop and waits for it to exit.
        /// </summary>
        public async Task StopAsync(CancellationToken token)
        {
            Stop();

            await WaitForExitAsync(token);
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
        /// Forces the process to stop
        /// </summary>
        private void Stop()
        {
            if (HasStarted && !_process.HasExited)
            {
                _process.Kill();
            }
        }
    }
}
