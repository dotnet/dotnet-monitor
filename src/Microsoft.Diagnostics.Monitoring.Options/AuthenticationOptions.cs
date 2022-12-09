﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class AuthenticationOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AuthenticationOptions_MonitorApiKey))]
        [Required]
        public MonitorApiKeyOptions MonitorApiKey { get; set; }
    }
}
