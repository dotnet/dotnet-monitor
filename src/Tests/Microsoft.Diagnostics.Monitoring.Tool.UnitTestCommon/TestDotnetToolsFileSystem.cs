// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.Extensibility;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal sealed class TestDotnetToolsFileSystem : IDotnetToolsFileSystem
    {
        public string Path { get; set; }
    }
}
