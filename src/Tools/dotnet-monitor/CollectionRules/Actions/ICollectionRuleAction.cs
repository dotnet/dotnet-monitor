﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal interface ICollectionRuleAction
    {
        Task StartAsync(CollectionRuleMetadata collectionRuleMetadata, CancellationToken token);

        Task StartAsync(CancellationToken token);

        Task<CollectionRuleActionResult> WaitForCompletionAsync(CancellationToken token);
    }

    internal struct CollectionRuleActionResult
    {
        public Dictionary<string, string> OutputValues { get; set; }
    }
}
