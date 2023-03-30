// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules
{
    internal static class KnownCollectionRuleTriggers
    {
        // Startup Triggers
        public const string Startup = nameof(Startup);

        // Event Source Triggers
        public const string AspNetRequestCount = nameof(AspNetRequestCount);
        public const string AspNetRequestDuration = nameof(AspNetRequestDuration);
        public const string AspNetResponseStatus = nameof(AspNetResponseStatus);
        public const string EventCounter = nameof(EventCounter);
        public const string EventMeter = nameof(EventMeter);

        // Shortcut Triggers
        public const string CPUUsage = nameof(CPUUsage);
        public const string GCHeapSize = nameof(GCHeapSize);
        public const string ThreadpoolQueueLength = nameof(ThreadpoolQueueLength);
    }
}
