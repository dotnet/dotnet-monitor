﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    public class CorsConfigurationOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CorsConfiguration_AllowedOrigins))]
        [Required]
        public string AllowedOrigins { get; set; }

        public string[] GetOrigins() => AllowedOrigins?.Split(';');
    }
}
