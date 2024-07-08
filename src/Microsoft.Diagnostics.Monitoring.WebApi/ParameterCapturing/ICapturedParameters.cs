// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Monitoring.WebApi.ParameterCapturing
{
    internal interface ICapturedParameters
    {
        string? ActivityId { get; }

        ActivityIdFormat ActivityIdFormat { get; }

        int ThreadId { get; }

        DateTime CapturedDateTime { get; }

        string ModuleName { get; }

        string TypeName { get; }

        string MethodName { get; }

        IReadOnlyList<ParameterInfo> Parameters { get; }
    }
}
