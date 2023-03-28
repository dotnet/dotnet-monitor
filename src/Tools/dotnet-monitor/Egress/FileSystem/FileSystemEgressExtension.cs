// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem
{
    /// <summary>
    /// Egress provider for egressing stream data to the file system.
    /// </summary>
    internal class FileSystemEgressExtension :
        IEgressExtension
    {
        private readonly ILogger<FileSystemEgressExtension> _logger;

        public string DisplayName => EgressProviderTypes.FileSystem;

        public FileSystemEgressExtension(ILogger<FileSystemEgressExtension> logger)
        {
            _logger = logger;
        }

        public async Task<EgressArtifactResult> EgressArtifact(
            ExtensionEgressPayload payload,
            Func<Stream, CancellationToken, Task> action,
            CancellationToken token)
        {
            FileSystemEgressProviderOptions options = new();
            Bind(options, payload.Configuration);

            if (!Directory.Exists(options.DirectoryPath))
            {
                WrapException(() => Directory.CreateDirectory(options.DirectoryPath));
            }

            string targetPath = Path.Combine(options.DirectoryPath, payload.Settings.Name);

            if (!string.IsNullOrEmpty(options.IntermediateDirectoryPath))
            {
                if (!Directory.Exists(options.IntermediateDirectoryPath))
                {
                    WrapException(() => Directory.CreateDirectory(options.IntermediateDirectoryPath));
                }

                string intermediateFilePath = null;
                try
                {
                    int remainingAttempts = 10;
                    bool intermediatePathExists;
                    do
                    {
                        intermediateFilePath = Path.Combine(options.IntermediateDirectoryPath, Path.GetRandomFileName());
                        intermediatePathExists = File.Exists(intermediateFilePath);
                        remainingAttempts--;
                    }
                    while (intermediatePathExists && remainingAttempts > 0);

                    if (intermediatePathExists)
                    {
                        throw CreateException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressUnableToCreateIntermediateFile, options.IntermediateDirectoryPath));
                    }

                    await WriteFileAsync(action, intermediateFilePath, token);

                    WrapException(() => File.Move(intermediateFilePath, targetPath));
                }
                finally
                {
                    // Attempt to delete the intermediate file if it exists.
                    try
                    {
                        if (File.Exists(intermediateFilePath))
                        {
                            File.Delete(intermediateFilePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.IntermediateFileDeletionFailed(intermediateFilePath, ex);
                    }
                }
            }
            else
            {
                await WriteFileAsync(action, targetPath, token);
            }

            _logger?.EgressProviderSavedStream(EgressProviderTypes.FileSystem, targetPath);

            return new EgressArtifactResult() { Succeeded = true, ArtifactPath = targetPath };
        }

        private async Task WriteFileAsync(Func<Stream, CancellationToken, Task> action, string filePath, CancellationToken token)
        {
            using Stream fileStream = WrapException(
                () => new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None));

            _logger?.EgressProviderInvokeStreamAction(EgressProviderTypes.FileSystem);

            await action(fileStream, token);

            await fileStream.FlushAsync(token);
        }

        private static void WrapException(Action action)
        {
            WrapException(() => { action(); return true; });
        }

        private static T WrapException<T>(Func<T> func)
        {
            try
            {
                return func();
            }
            catch (DirectoryNotFoundException ex)
            {
                throw CreateException(ex);
            }
            catch (PathTooLongException ex)
            {
                throw CreateException(ex);
            }
            catch (IOException ex)
            {
                throw CreateException(ex);
            }
            catch (NotSupportedException ex)
            {
                throw CreateException(ex);
            }
            catch (SecurityException ex)
            {
                throw CreateException(ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw CreateException(ex);
            }
        }

        private static EgressException CreateException(string message)
        {
            return new EgressException(WrapMessage(message));
        }

        private static EgressException CreateException(Exception innerException)
        {
            return new EgressException(WrapMessage(innerException.Message), innerException);
        }

        private static string WrapMessage(string innerMessage)
        {
            if (!string.IsNullOrEmpty(innerMessage))
            {
                return string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressFileFailedDetailed, innerMessage);
            }
            else
            {
                return Strings.ErrorMessage_EgressFileFailedGeneric;
            }
        }

        // This is a temporary stop-gap that simulates options binding from configuration. To do this properly, the extension
        // egress provider or service should pass the configuration directly to the IEgressExtension implementation and allow
        // it to decide how to pull information out of the configuration section.
        private static void Bind(FileSystemEgressProviderOptions options, IDictionary<string, string> configuration)
        {
            if (configuration.TryGetValue(nameof(FileSystemEgressProviderOptions.CopyBufferSize), out string copyBufferSizeString) &&
                !string.IsNullOrEmpty(copyBufferSizeString))
            {
                try
                {
                    options.CopyBufferSize = (int)TypeDescriptor.GetConverter(typeof(int)).ConvertFromInvariantString(copyBufferSizeString);
                }
                catch
                {
                }
            }

            if (configuration.TryGetValue(nameof(FileSystemEgressProviderOptions.DirectoryPath), out string directoryPath))
            {
                options.DirectoryPath = directoryPath;
            }

            if (configuration.TryGetValue(nameof(FileSystemEgressProviderOptions.IntermediateDirectoryPath), out string intermediateDirectoryPath))
            {
                options.IntermediateDirectoryPath = intermediateDirectoryPath;
            }
        }
    }
}
