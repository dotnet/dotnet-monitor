// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal interface ICollectionRuleAction<TOptions>
    {
        Task<CollectionRuleActionResult> ExecuteAsync(TOptions options, IProcessInfo processInfo, CancellationToken token);
    }

    internal struct CollectionRuleActionResult
    {
        public Dictionary<string, string> OutputValues { get; set; }
    }
}
