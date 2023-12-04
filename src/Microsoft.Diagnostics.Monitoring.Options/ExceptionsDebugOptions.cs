// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace Microsoft.Diagnostics.Monitoring.Options
{
    internal sealed class ExceptionsDebugOptions
    {
        [DefaultValue(ExceptionsDebugOptionsDefaults.IncludeMonitorExceptions)]
        public bool? IncludeMonitorExceptions { get; set; }
    }
}
