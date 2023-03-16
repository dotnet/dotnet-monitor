// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private Lazy<ExtensionDeclaration> _extensionDeclaration;
        private IDictionary<string, string> _processEnvironmentVariables = new Dictionary<string, string>();

        private static readonly TimeSpan WaitForProcessExitTimeout = TimeSpan.FromMilliseconds(2000);

        public ProgramExtension(string extensionName, string targetFolder, IFileProvider fileSystem, string declarationPath, ILogger<ProgramExtension> logger)
        {
            _extensionName = extensionName;
            _targetFolder = targetFolder;
            _fileSystem = fileSystem;
            _declarationPath = declarationPath;
            _exePath = declarationPath;
            _logger = logger;
            _extensionDeclaration = new Lazy<ExtensionDeclaration>(GetExtensionDeclaration, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public ProgramExtension(string extensionName, string targetFolder, IFileProvider fileSystem, string declarationPath, string exePath, ILogger<ProgramExtension> logger)
            : this(extensionName, targetFolder, fileSystem, declarationPath, logger)
        {
            _exePath = exePath;
        }

        /// <inheritdoc/>
        public string DisplayName => Path.GetDirectoryName(_declarationPath);

        public ExtensionDeclaration Declaration
        {
            get
            {
                return _extensionDeclaration.Value;
            }
        }

        public void AddEnvironmentVariable(string key, string value)
        {
            _processEnvironmentVariables[key] = value;
        }

        /// <inheritdoc/>
        public async Task<EgressArtifactResult> EgressArtifact(ExtensionEgressPayload configPayload, Func<Stream, CancellationToken, Task> getStreamAction, CancellationToken token)
        {
            if (!Declaration.SupportedExtensionTypes.Contains(ExtensionTypes.Egress))
            {
                ExtensionException.ThrowWrongType(_extensionName, _declarationPath, typeof(IEgressExtension));
            }

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
            if (Declaration.UseSharedDotNetHost)
            {
                executablePath = DotNetHost.ExecutablePath;

                string entrypointAssemblyRelativePath = Path.Combine(Path.GetDirectoryName(_exePath), $"{Declaration.Program}.dll");

                IFileInfo entrypointAssemblyInfo = _fileSystem.GetFileInfo(entrypointAssemblyRelativePath);
                ValidateExecutable(entrypointAssemblyInfo);

                pStart.ArgumentList.Add(entrypointAssemblyInfo.PhysicalPath);
            }
            else
            {
                string exeName = Declaration.Program;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    exeName += ".exe";
                }

                string programRelPath = Path.Combine(Path.GetDirectoryName(_exePath), exeName);

                IFileInfo executableInfo = _fileSystem.GetFileInfo(programRelPath);
                ValidateExecutable(executableInfo);
                executablePath = executableInfo.PhysicalPath;
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

            _logger.ExtensionStarting(pStart.FileName, pStart.Arguments);
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

        private ExtensionDeclaration GetExtensionDeclaration()
        {
            try
            {
                IFileInfo declFile = _fileSystem.GetFileInfo(_declarationPath);
                if (!declFile.Exists || declFile.IsDirectory)
                {
                    ExtensionException.ThrowNotFound(_extensionName);
                }

                using (Stream declStream = declFile.CreateReadStream())
                {
                    ExtensionDeclaration declResult = JsonSerializer.Deserialize<ExtensionDeclaration>(declStream);
                    return declResult;
                }
            }
            catch (Exception ex) when (LogBrokenDeclaration(ex))
            {
                // This will never get hit, LogBrokenDeclaration will never filter this exception
                // Do the logging in the filter so that the exception stack remains complete
                throw;
            }
        }

        private void ValidateExecutable(IFileInfo entrypointInfo)
        {
            if (!entrypointInfo.Exists || entrypointInfo.IsDirectory || entrypointInfo.PhysicalPath == null)
            {
                _logger.ExtensionProgramMissing(_extensionName, Path.Combine(_targetFolder, _exePath), Declaration.Program);
                ExtensionException.ThrowNotFound(_extensionName);
            }
        }

        private bool LogBrokenDeclaration(Exception ex)
        {
            _logger.ExtensionDeclarationFileBroken(_extensionName, Path.Combine(_targetFolder, _declarationPath), ex);
            return false;
        }
    }
}
