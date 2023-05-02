// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.LibrarySharing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Monitoring.Tool.TestHostingStartup
{
    internal sealed class BuildOutputSharedLibraryInitializer : ISharedLibraryInitializer
    {
        private readonly ILogger<BuildOutputSharedLibraryInitializer> _logger;

        public BuildOutputSharedLibraryInitializer(ILogger<BuildOutputSharedLibraryInitializer> logger)
        {
            _logger = logger;
        }

        public IFileProviderFactory Initialize()
        {
            _logger.SharedLibraryPath(BuildOutput.RootPath);

            return new Factory();
        }

        private sealed class Factory : IFileProviderFactory
        {
            public IFileProvider CreateManaged(string targetFramework)
            {
                return BuildOutputManagedFileProvider.Create(targetFramework, BuildOutput.RootPath);
            }

            public IFileProvider CreateNative(string runtimeIdentifier)
            {
                return BuildOutputNativeFileProvider.Create(runtimeIdentifier, BuildOutput.RootPath);
            }
        }
    }
}
