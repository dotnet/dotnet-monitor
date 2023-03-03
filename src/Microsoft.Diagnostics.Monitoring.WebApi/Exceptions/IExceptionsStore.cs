// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Exceptions
{
    internal interface IExceptionsStore
    {
        void AddExceptionInstance(IExceptionsNameCache cache, ulong exceptionId, string message);

        IReadOnlyList<IExceptionInstance> GetSnapshot();
    }
}
