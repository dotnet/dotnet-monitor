// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    /// <summary>
    /// Egress provider options for external egress providers.
    /// </summary>
    internal sealed partial class ExtensionEgressProviderOptions :
        Dictionary<string, string>,
        IEgressProviderCommonOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CommonEgressProviderOptions_CopyBufferSize))]
        public int? CopyBufferSize { get; set; }
    }
}
