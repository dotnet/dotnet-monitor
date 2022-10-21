﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
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
    internal class FileSystemEgressProvider :
        EgressProvider<FileSystemEgressProviderOptions>
    {
        ILogger<FileSystemEgressProvider> _logger;

        public FileSystemEgressProvider(ILogger<FileSystemEgressProvider> logger)
            : base(logger)
        {
            _logger = logger;
        }

        public override async Task<string> EgressAsync(
            string providerType,
            string providerName,
            FileSystemEgressProviderOptions options,
            Func<Stream, CancellationToken, Task> action,
            EgressArtifactSettings artifactSettings,
            CancellationToken token)
        {
            if (!Directory.Exists(options.DirectoryPath))
            {
                WrapException(() => Directory.CreateDirectory(options.DirectoryPath));
            }

            string targetPath = Path.Combine(options.DirectoryPath, artifactSettings.Name);

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

            Logger?.EgressProviderSavedStream(EgressProviderTypes.FileSystem, targetPath);
            return targetPath;
        }

        private async Task WriteFileAsync(Func<Stream, CancellationToken, Task> action, string filePath, CancellationToken token)
        {
            using Stream fileStream = WrapException(
                () => new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None));

            Logger?.EgressProviderInvokeStreamAction(EgressProviderTypes.FileSystem);
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
    }
}
