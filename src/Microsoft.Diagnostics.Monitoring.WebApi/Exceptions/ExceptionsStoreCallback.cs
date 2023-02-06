// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.WebApi.Exceptions
{
    internal abstract class ExceptionsStoreCallback
    {
        public virtual void OnExceptionInstance(ulong exceptionId, string message) { }
    }
}
