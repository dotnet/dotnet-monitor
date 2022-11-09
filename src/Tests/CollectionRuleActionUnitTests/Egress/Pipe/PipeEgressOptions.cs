// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO.Pipelines;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests.Egress.Pipe
{
    internal sealed class PipeEgressOptions
    {
        public PipeWriter Writer { get; set; }
    }
}
