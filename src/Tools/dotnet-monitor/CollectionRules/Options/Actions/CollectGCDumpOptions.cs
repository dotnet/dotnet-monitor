﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
#else
namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
#endif
{
    /// <summary>
    /// Options for the CollectGCDump action.
    /// </summary>
    [DebuggerDisplay("CollectGCDump")]
    internal sealed partial class CollectGCDumpOptions
    {
        [Required]
#if !UNITTEST
        [ValidateEgressProvider]
#endif
        public string Egress { get; set; }
    }
}
