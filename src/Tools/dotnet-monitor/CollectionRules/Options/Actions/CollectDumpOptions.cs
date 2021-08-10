// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
{
    /// <summary>
    /// Options for the CollectDump action.
    /// </summary>
    [DebuggerDisplay("CollectDump")]
    internal sealed partial class CollectDumpOptions
    {
        [EnumDataType(typeof(DumpType))]
        public DumpType? Type { get; set; }

        [Required]
#if !UNITTEST
        [ValidateEgressProvider]
#endif
        public string Egress { get; set; }
    }
}
