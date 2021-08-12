// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.NETCore.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules
{
    internal interface ICollectionRuleAction<TOptions>
    {
        Task<ActionResult> ExecuteAsync(TOptions options, DiagnosticsClient client, CancellationToken token);
    }

    internal struct ActionResult
    {
        internal Dictionary<string, string> OutputValues { get; set; }
    }
}
