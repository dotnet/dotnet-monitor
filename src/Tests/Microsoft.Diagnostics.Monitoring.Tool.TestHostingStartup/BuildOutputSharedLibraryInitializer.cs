// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.Tool.TestHostingStartup
{
    internal sealed class BuildOutputSharedLibraryInitializer : ISharedLibraryInitializer
    {
        private static readonly string SharedLibrarySourcePath =
            Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..", "..", ".."));

        private readonly ILogger<BuildOutputSharedLibraryInitializer> _logger;

        public BuildOutputSharedLibraryInitializer(ILogger<BuildOutputSharedLibraryInitializer> logger)
        {
            _logger = logger;
        }

        public INativeFileProviderFactory Initialize()
        {
            _logger.LogDebug("Shared Library Path: {path}", SharedLibrarySourcePath);

            return new Factory();
        }

        private class Factory : INativeFileProviderFactory
        {
            public IFileProvider Create(string runtimeIdentifier)
            {
                return BuildOutputNativeFileProvider.Create(SharedLibrarySourcePath, runtimeIdentifier);
            }
        }
    }
}
