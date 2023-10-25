// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.Extensibility;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal sealed class TestDotnetToolsFileSystem : IDotnetToolsFileSystem
    {
        public string Path { get; set; }
    }
}
