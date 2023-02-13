// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class AzureAdOptions
    {
        // https://learn.microsoft.com/en-us/azure/active-directory/develop/msal-client-application-configuration#cloud-instance
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MonitorApiKeyOptions_Subject))]
        [DefaultValue("https://login/microsoftonline.com")]
        [Required]
        public string Instance { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MonitorApiKeyOptions_Subject))]
        public string Domain { get; set; }

        // https://learn.microsoft.com/en-us/azure/active-directory/develop/msal-client-application-configuration#how-to-specify-the-audience-in-your-codeconfiguration
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MonitorApiKeyOptions_Subject))]
        [Required]
        public string TenantId { get; set; }

        // https://learn.microsoft.com/en-us/azure/active-directory/develop/msal-client-application-configuration#client-id
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MonitorApiKeyOptions_Subject))]
        [Required]
        public string ClientId { get; set; }

        // https://learn.microsoft.com/en-us/azure/active-directory/develop/msal-client-application-configuration#application-audience
        // https://github.com/AzureAD/microsoft-identity-web/wiki/web-apis#what-if-the-app-id-uri-of-your-application-is-not-apiclientid
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MonitorApiKeyOptions_Subject))]
        public string Audience { get; set; }

        // https://learn.microsoft.com/en-us/azure/active-directory/develop/msal-client-application-configuration#application-audience
        // https://github.com/AzureAD/microsoft-identity-web/wiki/web-apis#what-if-the-app-id-uri-of-your-application-is-not-apiclientid
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MonitorApiKeyOptions_Subject))]
        [Required]
        public string RequireRole { get; set; }

        // https://learn.microsoft.com/en-us/azure/active-directory/develop/msal-client-application-configuration#application-audience
        // https://github.com/AzureAD/microsoft-identity-web/wiki/web-apis#what-if-the-app-id-uri-of-your-application-is-not-apiclientid
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MonitorApiKeyOptions_Subject))]
        [Required]
        public string RequireScope { get; set; }
    }
}
