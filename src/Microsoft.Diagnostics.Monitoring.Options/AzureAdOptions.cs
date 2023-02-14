// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed partial class AzureAdOptions
    {
        private const string DefaultInstanceId = "https://login.microsoftonline.com";
        private const string DefaultTenantId = "organizations";

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureAdOptions_Instance))]
        [DefaultValue(DefaultInstanceId)]
        public string Instance { get; set; } = DefaultInstanceId;

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureAdOptions_TenantId))]
        [DefaultValue(DefaultTenantId)]
        public string TenantId { get; set; } = DefaultTenantId;

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureAdOptions_ClientId))]
        [Required]
        public string ClientId { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureAdOptions_Audience))]
        public string Audience { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureAdOptions_RequireRole))]
        public string RequireRole { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureAdOptions_RequireScope))]
        public string RequireScope { get; set; }
    }
}
