// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.Tool.TestHostingStartup
{
    internal sealed class BuildOutputSharedLibraryInitializer : ISharedLibraryInitializer
    {
        // This is the binary output directory when built from the dotnet-monitor repo: <repoRoot>/artifacts/bin
        private static readonly string SharedLibrarySourcePath =
            Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..", "..", ".."));

        private readonly ILogger<BuildOutputSharedLibraryInitializer> _logger;

        public BuildOutputSharedLibraryInitializer(ILogger<BuildOutputSharedLibraryInitializer> logger)
        {
            _logger = logger;
        }

        public INativeFileProviderFactory Initialize()
        {
            _logger.SharedLibraryPath(SharedLibrarySourcePath);

            return new Factory();
        }

        private sealed class Factory : INativeFileProviderFactory
        {
            public IFileProvider Create(string runtimeIdentifier)
            {
                return BuildOutputNativeFileProvider.Create(runtimeIdentifier, SharedLibrarySourcePath);
            }
        }
    }
}
