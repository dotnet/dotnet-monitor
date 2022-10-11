// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.FileProviders;

namespace Microsoft.Diagnostics.Tools.Monitor.Profiler
{
    internal interface INativeFileProviderFactory
    {
        IFileProvider Create(string runtimeIdentifier);
    }
}
