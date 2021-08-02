﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

#if UNITTEST
using Microsoft.Diagnostics.Monitoring.WebApi;

namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
#else
namespace Microsoft.Diagnostics.Monitoring.WebApi
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
}
