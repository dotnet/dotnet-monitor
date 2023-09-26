// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.LibrarySharing
{
    internal sealed class SharedLibraryService :
        BackgroundService,
        ISharedLibraryService
    {
        private readonly TaskCompletionSource<IFileProviderFactory> _fileProviderFactorySource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly IInProcessFeatures _inProcessFeatures;
        private readonly ISharedLibraryInitializer _sharedLibraryInitializer;
        private readonly ILogger<SharedLibraryService> _logger;

        public SharedLibraryService(
            ISharedLibraryInitializer sharedLibraryInitializer,
            IInProcessFeatures inProcessFeatures,
            ILogger<SharedLibraryService> logger)
        {
            _inProcessFeatures = inProcessFeatures;
            _logger = logger;
            _sharedLibraryInitializer = sharedLibraryInitializer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_inProcessFeatures.IsLibrarySharingRequired)
            {
                _fileProviderFactorySource.SetException(new NotSupportedException());
                return;
            }

            using IDisposable _ = stoppingToken.Register(() => _fileProviderFactorySource.TrySetCanceled(stoppingToken));

            // Yield to allow other hosting services to start
            await Task.Yield();

            try
            {
                _fileProviderFactorySource.SetResult(await _sharedLibraryInitializer.InitializeAsync(stoppingToken));
            }
            catch (Exception ex)
            {
                _logger.FailedInitializeSharedLibraryStorage(ex);

                _fileProviderFactorySource.SetException(ex);
            }
        }

        public Task<IFileProviderFactory> GetFactoryAsync(CancellationToken cancellationToken)
        {
            return _fileProviderFactorySource.Task.WaitAsync(cancellationToken);
        }
    }
}
