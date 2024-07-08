// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal class CollectionRuleMetadata
    {
        public string CollectionRuleName { get; set; } = string.Empty;

        public int ActionListIndex { get; set; }

        public string? ActionName { get; set; }
    }
}
