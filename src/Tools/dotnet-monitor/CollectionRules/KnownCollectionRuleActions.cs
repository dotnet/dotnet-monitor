﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules
{
    internal static class KnownCollectionRuleActions
    {
        public const string CollectDump = nameof(CollectDump);
        public const string CollectGCDump = nameof(CollectGCDump);
        public const string CollectLogs = nameof(CollectLogs);
        public const string CollectTrace = nameof(CollectTrace);
        public const string CollectLiveMetrics = nameof(CollectLiveMetrics);
        public const string Execute = nameof(Execute);
        public const string LoadProfiler = nameof(LoadProfiler);
        public const string SetEnvironmentVariable = nameof(SetEnvironmentVariable);
        public const string GetEnvironmentVariable = nameof(GetEnvironmentVariable);
    }
}
