// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Exceptions
{
    internal interface IExceptionInstance
    {
        ulong Id { get; }

        string Message { get; }

        string ModuleName { get; }

        string TypeName { get; }

        DateTime Timestamp { get; }

        CallStack? CallStack { get; }

        ulong[] InnerExceptionIds { get; }

        public string ActivityId { get; }

        public ActivityIdFormat ActivityIdFormat { get; }
    }
}
