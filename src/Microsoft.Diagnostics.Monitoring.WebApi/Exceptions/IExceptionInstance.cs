﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Exceptions
{
    internal interface IExceptionInstance
    {
        string Message { get; }

        string ModuleName { get; }

        string TypeName { get; }

        DateTime Timestamp { get; }

        CallStack CallStack { get; }
    }
}
