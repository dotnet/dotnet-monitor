// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Identification
{
    internal sealed class ExceptionGroupData
    {
        public ulong ExceptionClassId { get; set; }
        public ulong ThrowingMethodId { get; set; }
        public int ILOffset { get; set; }
    }
}
