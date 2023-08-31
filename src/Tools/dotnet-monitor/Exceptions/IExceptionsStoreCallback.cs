// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal interface IExceptionsStoreCallback
    {
        public void AfterAdd(IExceptionInstance instance);

        public void AfterRemove(IExceptionInstance instance);

        public void BeforeAdd(IExceptionInstance instance);
    }
}
