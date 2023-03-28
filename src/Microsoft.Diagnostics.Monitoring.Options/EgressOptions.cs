// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class EgressOptions
    {
        [Display(
            Description = "Mapping of file system egress provider names to their options.")]
        public IDictionary<string, FileSystemEgressProviderOptions> FileSystem { get; set; }

        [Display(
            Description = "Additional properties, such as secrets, that can be referenced by the provider definitions.")]
        public IDictionary<string, string> Properties { get; set; }
    }
}
