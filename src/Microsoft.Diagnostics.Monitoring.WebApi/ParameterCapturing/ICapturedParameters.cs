// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.WebApi.ParameterCapturing
{
    internal interface ICapturedParameters
    {
        Guid RequestId { get; }

        public string ActivityId { get; }

        public DateTime CapturedDateTime { get; }

        public string ModuleName { get; }

        public string TypeName { get; }

        public string MethodName { get; }

        IReadOnlyList<ParameterInfo> Parameters { get; }
    }
}
