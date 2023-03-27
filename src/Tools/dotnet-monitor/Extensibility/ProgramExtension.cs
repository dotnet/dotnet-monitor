// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    [DebuggerDisplay("Extension {_extensionName,nq} @ {_declarationPath}")]
    internal partial class ProgramExtension : IExtension, IEgressExtension
    {
        private readonly string _extensionName;
        private readonly string _targetFolder;
        private readonly string _declarationPath;
        private readonly string _exePath;
        private readonly IFileProvider _fileSystem;
        private readonly ILogger<ProgramExtension> _logger;
        private ExtensionManifest _manifest;
        private IDictionary<string, string> _processEnvironmentVariables = new Dictionary<string, string>();

        private static readonly TimeSpan WaitForProcessExitTimeout = TimeSpan.FromMilliseconds(2000);

        public ProgramExtension(ExtensionManifest manifest, string extensionName, string targetFolder, IFileProvider fileSystem, string declarationPath, ILogger<ProgramExtension> logger)
        {
            _extensionName = extensionName;
            _targetFolder = targetFolder;
            _fileSystem = fileSystem;
            _declarationPath = declarationPath;
            _exePath = declarationPath;
            _logger = logger;
            _manifest = manifest;
        }

        /// <inheritdoc/>
        public string DisplayName => Path.GetDirectoryName(_declarationPath);

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

                string entrypointAssemblyPath = Path.Combine(Path.GetDirectoryName(_exePath), $"{_manifest.AssemblyFileName}.dll");

                IFileInfo entrypointAssemblyInfo = new PhysicalFileInfo(new FileInfo(entrypointAssemblyPath));
                ValidateExecutable(entrypointAssemblyInfo);

                pStart.ArgumentList.Add(entrypointAssemblyInfo.PhysicalPath);
            }
            else if (!string.IsNullOrEmpty(_manifest.ExecutableFileName))
            {
                string exeName = _manifest.ExecutableFileName;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    exeName += ".exe";
                }

                string programPath = Path.Combine(Path.GetDirectoryName(_exePath), exeName);

                IFileInfo executableInfo = new PhysicalFileInfo(new FileInfo(programPath));
                ValidateExecutable(executableInfo);
                executablePath = executableInfo.PhysicalPath;
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

            _logger.ExtensionStarting(_extensionName);
            if (!p.Start())
            {
                ExtensionException.ThrowLaunchFailure(_extensionName);
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

        private void ValidateExecutable(IFileInfo entrypointInfo)
        {
            if (!entrypointInfo.Exists || entrypointInfo.IsDirectory || entrypointInfo.PhysicalPath == null)
            {
                _logger.ExtensionProgramMissing(_extensionName, Path.Combine(_targetFolder, _exePath), _manifest.ExecutableFileName);
                ExtensionException.ThrowNotFound(_extensionName);
            }
        }
    }
}
