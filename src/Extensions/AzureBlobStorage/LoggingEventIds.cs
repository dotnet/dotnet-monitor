// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Monitoring.AzureBlobStorage
{
    // The existing EventIds must not be duplicated, reused, or repurposed.
    // New logging events must use the next available EventId.
    internal enum LoggingEventIds
    {
        EgressProvideUnableToFindPropertyKey = 1,
        EgressProviderInvokeStreamAction = 2,
        QueueDoesNotExist = 3,
        QueueOptionsPartiallySet = 4,
        WritingMessageToQueueFailed = 5,
        InvalidMetadata = 6,
        DuplicateKeyInMetadata = 7,
        EnvironmentVariableNotFound = 8,
        EnvironmentBlockNotSupported = 9,
        EgressProviderSavedStream = 10,
        EgressCopyActionStreamToEgressStream = 11
    }

    internal static class LoggingEventIdsExtensions
    {
        public static EventId EventId(this LoggingEventIds enumVal)
        {
            string name = Enum.GetName(typeof(LoggingEventIds), enumVal);
            int id = enumVal.Id();
            return new EventId(id, name);
        }
        public static int Id(this LoggingEventIds enumVal)
        {
            int id = (int)enumVal;
            return id;
        }
    }
}
