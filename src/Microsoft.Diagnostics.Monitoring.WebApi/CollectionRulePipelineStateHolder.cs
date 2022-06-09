// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal record class CollectionRulePipelineStateHolder
    {
        public CollectionRuleStateHolder StateHolder { get; set; }
        public Queue<DateTime> ExecutionTimestamps { get; set; }
    }
}
