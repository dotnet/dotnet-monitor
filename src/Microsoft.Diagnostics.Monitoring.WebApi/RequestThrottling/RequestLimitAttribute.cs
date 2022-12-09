// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    internal sealed class RequestLimitAttribute : Attribute
    {
        public string LimitKey { get; set; }
    }
}
