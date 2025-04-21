// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed partial class AzureAdOptions
    {
        [Display(
            Name = nameof(Instance),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureAdOptions_Instance))]
        [DefaultValue(AzureAdOptionsDefaults.DefaultInstance)]
        public Uri? Instance { get; set; }

        [Display(
            Name = nameof(TenantId),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureAdOptions_TenantId))]
        [Required]
        public string TenantId { get; set; } = string.Empty;

        [Display(
            Name = nameof(ClientId),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureAdOptions_ClientId))]
        [Required]
        public string ClientId { get; set; } = string.Empty;

        [Display(
            Name = nameof(AppIdUri),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureAdOptions_AppIdUri))]
        public Uri? AppIdUri { get; set; }

        [Display(
            Name = nameof(RequiredRole),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureAdOptions_RequiredRole))]
        [Required]
        public string RequiredRole { get; set; } = string.Empty;
    }
}
