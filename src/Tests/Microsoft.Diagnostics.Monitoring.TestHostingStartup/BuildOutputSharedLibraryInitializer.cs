// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Diagnostics.Monitoring.TestHostingStartup
{
    internal sealed class BuildOutputSharedLibraryInitializer : ISharedLibraryInitializer
    {
        public INativeFileProviderFactory Initialize()
        {
            return new Factory();
        }

        private class Factory : INativeFileProviderFactory
        {
            public IFileProvider Create(string runtimeIdentifier)
            {
                return BuildOutputNativeFileProvider.Create(runtimeIdentifier);
            }
        }
    }
}
