// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Exceptions
{
    internal record class ExceptionInstance(ulong Id, string TypeName, string ModuleName, string Message, DateTime Timestamp, CallStack? CallStack, ulong[] InnerExceptionIds, string ActivityId, ActivityIdFormat ActivityIdFormat)
        : IExceptionInstance
    {
    }
}
