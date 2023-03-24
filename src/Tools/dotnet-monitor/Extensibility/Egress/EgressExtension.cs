// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility.Egress
{
    [DebuggerDisplay("{_manifest.Name,nq} Extension ({_extensionPath})")]
    internal partial class EgressExtension : IExtension, IEgressExtension
    {
        private readonly string _extensionPath;
        private readonly ILogger<EgressExtension> _logger;
        private ExtensionManifest _manifest;
        private IDictionary<string, string> _processEnvironmentVariables = new Dictionary<string, string>();

        private static readonly TimeSpan WaitForProcessExitTimeout = TimeSpan.FromMilliseconds(2000);

        public EgressExtension(ExtensionManifest manifest, string extensionPath, ILogger<EgressExtension> logger)
        {
            _extensionPath = extensionPath ?? throw new ArgumentNullException(nameof(extensionPath));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        }

        /// <inheritdoc/>
        public string DisplayName => _extensionPath;

        public void AddEnvironmentVariable(string key, string value)
        {
            _processEnvironmentVariables[key] = value;
        }

        /// <inheritdoc/>
        public async Task<EgressArtifactResult> EgressArtifact(ExtensionEgressPayload configPayload, Func<Stream, CancellationToken, Task> getStreamAction, CancellationToken token)
        {
            _manifest.Validate();

            // This is really weird, yes, but this is one of 2 overloads for [Stream].WriteAsync(...) that supports a CancellationToken, so we use a ReadOnlyMemory<char> instead of a string.
            ReadOnlyMemory<char> NewLine = new ReadOnlyMemory<char>("\r\n".ToCharArray());

            ProcessStartInfo pStart = new ProcessStartInfo()
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            string executablePath;
            if (!string.IsNullOrEmpty(_manifest.AssemblyFileName))
            {
                executablePath = DotNetHost.ExecutablePath;

                string assemblyPath = Path.Combine(_extensionPath, $"{_manifest.AssemblyFileName}.dll");

                ValidateFileExists(new FileInfo(assemblyPath));

                pStart.ArgumentList.Add(assemblyPath);
            }
            else if (!string.IsNullOrEmpty(_manifest.ExecutableFileName))
            {
                string exeName = _manifest.ExecutableFileName;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    exeName += ".exe";
                }

                executablePath = Path.Combine(_extensionPath, exeName);

                ValidateFileExists(new FileInfo(executablePath));
            }
            else
            {
                // Should never reach this point because validation should have checked this.
                // This is a logical error in dotnet-monitor if execution reaches here.
                throw new InvalidOperationException();
            }

            /* [TODOs]
             * 1. [Done] Add a new service to dynamically find these extension(s)
             * 2. [Done] Remove all raw logging statements from this method and refactor into LoggingExtensions
             * 3. [Done] Stream StdOut and StdErr async in the process so their streams don't need to end before we can return
             * 4. [Done] Refactor WaitForExit to do an async wait
             * 5. [Simple first part done] Add well-factored protocol for returning information from an extension
             */
            pStart.FileName = executablePath;
            pStart.ArgumentList.Add(ExtensionTypes.Egress);

            foreach ((string key, string value) in _processEnvironmentVariables)
            {
                pStart.Environment.Add(key, value);
            }

            using Process p = new Process()
            {
                StartInfo = pStart,
            };

            using OutputParser<EgressArtifactResult> parser = new(p, _logger);

            _logger.ExtensionStarting(_manifest.Name);
            if (!p.Start())
            {
                ExtensionException.ThrowLaunchFailure(_manifest.Name);
            }

            parser.BeginReading();

            await JsonSerializer.SerializeAsync<ExtensionEgressPayload>(p.StandardInput.BaseStream, configPayload, options: null, token);
            await p.StandardInput.WriteAsync(NewLine, token);
            await p.StandardInput.BaseStream.FlushAsync(token);
            _logger.ExtensionConfigured(pStart.FileName, p.Id);

            await getStreamAction(p.StandardInput.BaseStream, token);
            await p.StandardInput.BaseStream.FlushAsync(token);
            p.StandardInput.Close();
            _logger.ExtensionEgressPayloadCompleted(p.Id);

            EgressArtifactResult result = await parser.ReadResult();

            using var timeoutSource = new CancellationTokenSource();

            try
            {
                timeoutSource.CancelAfter(WaitForProcessExitTimeout);

                using CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutSource.Token);

                await p.WaitForExitAsync(linkedTokenSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
            {
                p.Kill();

                await p.WaitForExitAsync(token);
            }

            _logger.ExtensionExited(p.Id, p.ExitCode);

            return result;
        }

        private void ValidateFileExists(FileInfo fileInfo)
        {
            if (!fileInfo.Exists)
            {
                ExtensionException.ThrowFileNotFound(_manifest.Name, fileInfo.FullName);
            }
        }
    }
}
