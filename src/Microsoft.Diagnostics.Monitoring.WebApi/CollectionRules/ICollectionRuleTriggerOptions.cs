// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Options for describing the type of trigger and the settings to pass to that trigger.
    /// </summary>
    public class CollectionRuleTriggerOptions
    {
        [Required]
        public string Type { get; set; }

        public object Settings { get; internal set; }
    }
}
