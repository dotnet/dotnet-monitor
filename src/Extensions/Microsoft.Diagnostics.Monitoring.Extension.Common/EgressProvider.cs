// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.Extension.Common
{
    internal abstract class EgressProvider<TOptions> where TOptions : class
    {
        protected ILogger Logger { get; set; }

        public EgressProvider() { }

        public abstract Task<string> EgressAsync(
            ILogger logger,
            TOptions options,
            Func<Stream, CancellationToken, Task> action,
            EgressArtifactSettings artifactSettings,
            CancellationToken token);
    }
}
