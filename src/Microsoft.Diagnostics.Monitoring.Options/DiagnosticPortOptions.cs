// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    public class DiagnosticPortOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_DiagnosticPortOptions_ConnectionMode))]
        [DefaultValue(DiagnosticPortOptionsDefaults.ConnectionMode)]
        public DiagnosticPortConnectionMode? ConnectionMode { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_DiagnosticPortOptions_EndpointName))]
        public string? EndpointName { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_DiagnosticPortOptions_MaxConnections))]
        public int? MaxConnections { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_DiagnosticPortOptions_DeleteEndpointOnStartup))]
        [DefaultValue(DiagnosticPortOptionsDefaults.DeleteEndpointOnStartup)]
        public bool? DeleteEndpointOnStartup { get; set; }
    }
}
