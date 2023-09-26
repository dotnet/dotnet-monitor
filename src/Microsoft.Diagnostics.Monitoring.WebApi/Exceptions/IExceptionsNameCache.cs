// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Stacks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Exceptions
{
    internal interface IExceptionsNameCache
    {
        public bool TryGetExceptionGroup(ulong groupId, out ulong exceptionClassId, out ulong throwingMethodId, out int ilOffset);
        public bool TryGetStackFrameIds(ulong stackFrameId, out ulong methodId, out int ilOffset);

        NameCache NameCache { get; }
    }
}
