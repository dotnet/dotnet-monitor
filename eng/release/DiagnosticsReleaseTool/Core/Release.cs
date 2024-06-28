// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReleaseTool.Core
{
    public class Release : IDisposable
    {
        // TODO: there might be a need to expose this for multiple product roots.
        private readonly DirectoryInfo _productBuildPath;
        private readonly List<ILayoutWorker> _layoutWorkers;
        private readonly List<IReleaseVerifier> _verifiers;
        private readonly IPublisher _publisher;
        private readonly IManifestGenerator _manifestGenerator;
        private readonly string _manifestSavePath;

        private readonly List<FileReleaseData> _filesToRelease;
        private ILogger _logger;

        public Release(DirectoryInfo productBuildPath,
            List<ILayoutWorker> layoutWorkers, List<IReleaseVerifier> verifiers,
            IPublisher publisher, IManifestGenerator manifestGenerator, string manifestSavePath)
        {
            if (productBuildPath is null)
            {
                throw new ArgumentException("Product build path can't be empty or null.");
            }

            if (layoutWorkers is null)
            {
                throw new ArgumentException($"{nameof(layoutWorkers)} can't be null.");
            }

            if (publisher is null)
            {
                throw new ArgumentException($"{nameof(publisher)} can't be null.");
            }

            if (manifestGenerator is null)
            {
                throw new ArgumentException($"{nameof(manifestGenerator)} can't be null.");
            }

            _productBuildPath = productBuildPath;
            _layoutWorkers = layoutWorkers;
            _verifiers = verifiers;
            _publisher = publisher;
            _manifestGenerator = manifestGenerator;
            _manifestSavePath = manifestSavePath ?? Path.Join(Path.GetTempPath(), Path.GetRandomFileName(), "releaseManifest");
            _filesToRelease = new List<FileReleaseData>();
            _logger = null;
            // TODO: Validate drop to publish exists.
        }

        public void UseLogger(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<int> RunAsync(CancellationToken ct)
        {
            EnsureLoggerAvailable();

            int unusedFiles = 0;
            try
            {
                unusedFiles = await LayoutFilesAsync(ct);

                // TODO: Implement switch to ignore files that are not used as option.
                if (unusedFiles > 0)
                {
                    _logger.LogError("{unusedFiles} files were not handled for release.", unusedFiles);
                    return unusedFiles;
                }

                if (unusedFiles < 0)
                {
                    _logger.LogError("Error processing file layout for release.");
                    return unusedFiles;
                }

                // TODO: Verification

                unusedFiles = await PublishFiles(ct);
                if (unusedFiles > 0)
                {
                    _logger.LogError("{unusedFiles} files were not published.", unusedFiles);
                    return unusedFiles;
                }
                if (unusedFiles < 0)
                {
                    _logger.LogError("Error processing publish files for release.");
                    return unusedFiles;
                }

                return await GenerateAndPublishManifestAsync(ct);
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("Cancellation issued.");
                return -1;
            }
            catch (AggregateException agEx)
            {
                _logger.LogError("Aggregate Exception");

                foreach (var ex in agEx.InnerExceptions)
                    _logger.LogError(ex, "Inner Exception");

                return -1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception");
                return -1;
            }
        }

        private void EnsureLoggerAvailable() => _logger ??= NullLogger.Instance;

        private async Task<int> GenerateAndPublishManifestAsync(CancellationToken ct)
        {
            // Manifest
            using IDisposable scope = _logger.BeginScope("Manifest Generation");
            Stream manifestStream = _manifestGenerator.GenerateManifest(_filesToRelease);
            var fi = new FileInfo(_manifestSavePath);
            fi.Directory.Create();

            using (FileStream fs = fi.Open(FileMode.Create, FileAccess.Write))
            {
                await manifestStream.CopyToAsync(fs, ct);
            }

            _logger.LogInformation("Manifest saved to {_manifestSavePath}", _manifestSavePath);

            // We save the manifest at the root.
            string manifestPublishPath = await _publisher.PublishFileAsync(
                new FileMapping(_manifestSavePath, fi.Name),
                ct
            );

            if (manifestPublishPath is null)
            {
                _logger.LogError("Couldn't publish manifest");
                return -1;
            }

            _logger.LogInformation("Published manifest to {manifestPublishPath}", manifestPublishPath);

            return 0;
        }

        private async Task<int> PublishFiles(CancellationToken ct)
        {
            int unpublishedFiles = 0;

            using IDisposable scope = _logger.BeginScope("Publishing files");
            _logger.LogInformation("Publishing {fileCount} files", _filesToRelease.Count);

            for (int i = 0; i < _filesToRelease.Count; i++)
            {
                FileReleaseData releaseData = _filesToRelease[i];
                if (ct.IsCancellationRequested)
                {
                    _logger.LogError("[{ind}: {srcPath} -> {dstPath}, {fileMetadata}] Cancellation issued.", i, releaseData.FileMap.LocalSourcePath, releaseData.FileMap.RelativeOutputPath, releaseData.FileMetadata);
                    return -1;
                }

                string sourcePath = releaseData.FileMap.LocalSourcePath;
                string relOutputPath = releaseData.FileMap.RelativeOutputPath;
                string publishUri = await _publisher.PublishFileAsync(releaseData.FileMap, ct);
                if (publishUri is null)
                {
                    _logger.LogWarning("[{ind}: {srcPath} -> {dstPath}, {fileMetadata}] Failed to publish {sourcePath}", i, releaseData.FileMap.LocalSourcePath, releaseData.FileMap.RelativeOutputPath, releaseData.FileMetadata, sourcePath);
                    unpublishedFiles++;
                }
                else
                {
                    _logger.LogTrace("[{ind}: {srcPath} -> {dstPath}, {fileMetadata}] Published {sourcePath} to relative path {relOutputPath} at {publishUri}", i, releaseData.FileMap.LocalSourcePath, releaseData.FileMap.RelativeOutputPath, releaseData.FileMetadata, sourcePath, relOutputPath, publishUri);
                    releaseData.PublishUri = publishUri;
                }
            }

            return unpublishedFiles;
        }

        private async Task<int> LayoutFilesAsync(CancellationToken ct)
        {
            int unhandledFiles = 0;
            var relativePublishPathsToHash = new Dictionary<string, string>();

            using var scope = _logger.BeginScope("Laying out files");

            _logger.LogInformation("Laying out files from {_productBuildPath}", _productBuildPath.Name);

            // TODO: Make this parallel using Task.Run + semaphore to batch process files. Need to make collections concurrent or have single
            //       queue to aggregate results.
            // TODO: The file enumeration should have the possibility to inject a custom enumerator. Useful in case there's only subsets of files.
            //       For example, shipping only files. 
            foreach (FileInfo file in _productBuildPath.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                bool isProcessed = false;
                foreach (ILayoutWorker worker in _layoutWorkers)
                {
                    if (ct.IsCancellationRequested)
                    {
                        _logger.LogError("[{buildFilePath}, {worker}] Cancellation issued.", file.FullName, worker.GetType().FullName);
                        return -1;
                    }

                    // Do we want to parallelize the number of workers?
                    LayoutWorkerResult layoutResult = await worker.HandleFileAsync(file, ct);

                    if (layoutResult.Status == LayoutResultStatus.Error)
                    {
                        _logger.LogError("[{buildFilePath}, {worker}] Error handling file.", file.FullName, worker.GetType().FullName);
                        return -1;
                    }

                    if (layoutResult.Status == LayoutResultStatus.FileHandled)
                    {
                        isProcessed = true;

                        (FileMapping fileMap, FileMetadata fileMetadata)[] layoutResultArray = layoutResult.LayoutDataEnumerable.ToArray();
                        for (int i = 0; i < layoutResultArray.Length; i++)
                        {
                            (FileMapping fileMap, FileMetadata fileMetadata) = layoutResultArray[i];
                            string srcPath = fileMap.LocalSourcePath;
                            string dstPath = fileMap.RelativeOutputPath;
                            if (relativePublishPathsToHash.ContainsKey(dstPath))
                            {
                                if (!string.IsNullOrEmpty(fileMetadata.Sha512) &&
                                    !string.IsNullOrEmpty(relativePublishPathsToHash[dstPath]) &&
                                    relativePublishPathsToHash[dstPath] == fileMetadata.Sha512)
                                {
                                    _logger.LogInformation("[{buildFilePath}, {worker}, {layoutInd}: {srcPath} -> {dstPath}, {fileMetadata}] File already published to {dstPath} with same hash: {hash}. This is being allowed.", file.FullName, worker.GetType().FullName, i, srcPath, dstPath, fileMetadata, dstPath, fileMetadata.Sha512);
                                }
                                else
                                {
                                    _logger.LogError("[{buildFilePath}, {worker}, {layoutInd}: {srcPath} -> {dstPath}, {fileMetadata}] Destination path {dstPath2} already in use and hashes do not match (or hashes are empty). Published file hash: {hash1}; File attempted to publish: {hash2}", file.FullName, worker.GetType().FullName, i, srcPath, dstPath, fileMetadata, dstPath, relativePublishPathsToHash[dstPath], fileMetadata.Sha512);
                                    return -1;
                                }
                            }
                            else
                            {
                                relativePublishPathsToHash.Add(dstPath, fileMetadata.Sha512);
                            }
                            _logger.LogTrace("[{buildFilePath}, {worker}, {layoutInd}: {srcPath} -> {dstPath}, {fileMetadata}] adding layout to release data.", file.FullName, worker.GetType().FullName, i, srcPath, dstPath, fileMetadata);
                            _filesToRelease.Add(new FileReleaseData(fileMap, fileMetadata));
                        }

                        // Skip remaining layout workers
                        break;
                    }
                }

                if (!isProcessed)
                {
                    _logger.LogWarning("[{buildFilePath}] File not handled {file}", file.FullName, file);
                    unhandledFiles++;
                }
            }

            return unhandledFiles;
        }

        public void Dispose()
        {
            foreach (ILayoutWorker lw in _layoutWorkers)
                lw.Dispose();

            foreach (IReleaseVerifier rv in _verifiers)
                rv.Dispose();

            _publisher.Dispose();
            _manifestGenerator.Dispose();
        }
    }
}
