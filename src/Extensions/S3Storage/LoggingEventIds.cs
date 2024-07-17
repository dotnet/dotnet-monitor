// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Diagnostics.Monitoring.Extension.S3Storage
{
    // The existing EventIds must not be duplicated, reused, or repurposed.
    // New logging events must use the next available EventId.
    internal enum LoggingEventIds
    {
        EgressProviderInvokeStreamAction = 1,
        EgressProviderSavedStream = 2
    }

    internal static class LoggingEventIdsExtensions
    {
        public static EventId EventId(this LoggingEventIds enumVal)
        {
            string? name = Enum.GetName(typeof(LoggingEventIds), enumVal);
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
