// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.UnitTests.Options
#else
namespace Microsoft.Diagnostics.Tools.Monitor
#endif
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
        public string EndpointName { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_DiagnosticPortOptions_MaxConnections))]
        public int? MaxConnections { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DiagnosticPortConnectionMode
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_DiagnosticPortConnectionMode_Connect))]
        Connect,

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_DiagnosticPortConnectionMode_Listen))]
        Listen
    }
}
