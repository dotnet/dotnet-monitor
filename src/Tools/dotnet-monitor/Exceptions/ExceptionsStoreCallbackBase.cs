// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal abstract class ExceptionsStoreCallbackBase :
        IExceptionsStoreCallback
    {
        public virtual void AfterAdd(IExceptionInstance instance) { }

        public virtual void AfterRemove(IExceptionInstance instance) { }

        public virtual void BeforeAdd(IExceptionInstance instance) { }
    }
}
