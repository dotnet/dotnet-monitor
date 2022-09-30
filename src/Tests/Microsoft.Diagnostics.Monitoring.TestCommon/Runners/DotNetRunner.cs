// Licensed to the .NET Foundation under one or more agreements.
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
        public Process StartedProcess { get; }

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
        public IDictionary<string, string> Environment => StartedProcess.StartInfo.Environment;

        /// <summary>
        /// Gets a <see cref="bool"/> indicating if <see cref="StartAsync(CancellationToken)"/> has been called and the process has been started.
        /// </summary>
        public bool HasStarted { get; private set; } = false;

        /// <summary>
        /// Retrieves the exit code of the process.
        /// </summary>
        public int ExitCode => StartedProcess.ExitCode;

        /// <summary>
        /// Gets a task that completes with the process exit code when it exits.
        /// </summary>
        public Task<int> ExitedTask => _exitedSource.Task;

        /// <summary>
        /// The framework reference of the app to run.
        /// </summary>
        public DotNetFrameworkReference FrameworkReference { get; set; } = DotNetFrameworkReference.Microsoft_NetCore_App;

        /// <summary>
        /// Determines if the process has exited.
        /// </summary>
        public bool HasExited => HasStarted && StartedProcess.HasExited;

        /// <summary>
        /// Gets the process ID of the running process.
        /// </summary>
        public int ProcessId => StartedProcess.Id;

        /// <summary>
        /// Gets a <see cref="StreamReader"/> that reads stderr.
        /// </summary>
        public StreamReader StandardError => StartedProcess.StandardError;

        /// <summary>
        /// Gets a <see cref="StreamWriter"/> that writes to stdin.
        /// </summary>
        public StreamWriter StandardInput => StartedProcess.StandardInput;

        /// <summary>
        /// Gets a <see cref="StreamReader"/> that reads stdout.
        /// </summary>
        public StreamReader StandardOutput => StartedProcess.StandardOutput;

        /// <summary>
        /// Get or set the target framework on which the application should run.
        /// </summary>
        public TargetFrameworkMoniker TargetFramework { get; set; } = TargetFrameworkMoniker.Current;

        /// <summary>
        /// Determines if <see cref="StartAsync(CancellationToken)" /> should wait for the diagnostic pipe to be available.
        /// </summary>
        public bool WaitForDiagnosticPipe { get; set; }

        public DotNetRunner()
        {
            StartedProcess = new Process();
            StartedProcess.StartInfo.FileName = DotNetHost.HostExePath;
            StartedProcess.StartInfo.UseShellExecute = false;
            StartedProcess.StartInfo.RedirectStandardError = true;
            StartedProcess.StartInfo.RedirectStandardInput = true;
            StartedProcess.StartInfo.RedirectStandardOutput = true;

            _exitedSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            _exitedHandler = (s, e) => _exitedSource.SetResult(StartedProcess.ExitCode);

            StartedProcess.EnableRaisingEvents = true;
            StartedProcess.Exited += _exitedHandler;
        }

        public void Dispose()
        {
            ForceClose();

            StartedProcess.Dispose();
        }

        /// <summary>
        /// Starts the dotnet process.
        /// </summary>
        public async Task StartAsync(CancellationToken token)
        {
            string frameworkVersion = null;
            switch (FrameworkReference)
            {
                case DotNetFrameworkReference.Microsoft_AspNetCore_App:
                    // Starting in .NET 6, the .NET SDK is emitting two framework references
                    // into the .runtimeconfig.json file. This is preventing the --fx-version
                    // parameter from having the correct effect of using the exact framework version
                    // that we want. Disabling this forced version usage for ASP.NET 6+ applications
                    // until it can be resolved.
                    if (!TargetFramework.IsEffectively(TargetFrameworkMoniker.Net60) &&
                        !TargetFramework.IsEffectively(TargetFrameworkMoniker.Net70))
                    {
                        frameworkVersion = TargetFramework.GetAspNetCoreFrameworkVersionString();
                    }
                    break;
                case DotNetFrameworkReference.Microsoft_NetCore_App:
                    frameworkVersion = TargetFramework.GetNetCoreAppFrameworkVersionString();
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported framework reference: {FrameworkReference}");
            }

            StringBuilder argsBuilder = new();
            if (!string.IsNullOrEmpty(frameworkVersion))
            {
                argsBuilder.Append("--fx-version ");
                argsBuilder.Append(frameworkVersion);
                argsBuilder.Append(" ");
            }
            argsBuilder.Append("\"");
            argsBuilder.Append(EntrypointAssemblyPath);
            argsBuilder.Append("\" ");
            argsBuilder.Append(Arguments);

            var pathSections = EntrypointAssemblyPath.Split('\\');
            if (pathSections[pathSections.Length - 1] == "Microsoft.Diagnostics.Monitoring.UnitTestWebApp.dll")
            {
                argsBuilder.Append(" --urls http://0.0.0.0:82");
            }

            StartedProcess.StartInfo.Arguments = argsBuilder.ToString();

            if (!StartedProcess.Start())
            {
                throw new InvalidOperationException($"Unable to start: {StartedProcess.StartInfo.FileName} {StartedProcess.StartInfo.Arguments}");
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

                        var matchingFiles = Directory.GetFiles(Path.GetTempPath(), $"dotnet-diagnostic-{StartedProcess.Id}-*-socket");
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
            if (HasStarted && !StartedProcess.HasExited)
            {
                await ExitedTask.WithCancellation(token);
            }
        }

        /// <summary>
        /// Forces the process to exit.
        /// </summary>
        public void ForceClose()
        {
            if (HasStarted && !StartedProcess.HasExited)
            {
                StartedProcess.Kill();
            }
        }
    }
}
