// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class ActionListExecutor
    {
        public async Task<CollectionRuleActionResult> ExecuteActions(List<CollectionRuleActionOptions> collectionRuleActionOptions, IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            foreach (CollectionRuleActionOptions actionOption in collectionRuleActionOptions)
            {


                Type.GetType(actionOption.Type) options = actionOption.Settings;
            }
        }
    }
}