// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Exceptions
{
    internal interface IExceptionsStore
    {
        void AddExceptionInstance(
            IExceptionsNameCache cache,
            ulong exceptionId,
            ulong groupId,
            string message,
            DateTime timestamp,
            ulong[] stackFrameIds,
            int threadId,
            ulong[] innerExceptionIds,
            string activityId,
            ActivityIdFormat activityIdFormat);

        void RemoveExceptionInstance(ulong exceptionId);

        IReadOnlyList<IExceptionInstance> GetSnapshot();
    }
}
