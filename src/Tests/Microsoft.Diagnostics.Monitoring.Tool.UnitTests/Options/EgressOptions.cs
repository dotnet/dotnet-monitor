// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.UnitTests.Options
{
    internal class EgressOptions
    {
        [Display(
            ResourceType = typeof(SharedStrings),
            Description = nameof(SharedStrings.DisplayAttributeDescription_EgressOptions_Providers))]
        public Dictionary<string, EgressProvider> Providers { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        [Display(
            ResourceType = typeof(SharedStrings),
            Description = nameof(SharedStrings.DisplayAttributeDescription_EgressOptions_Properties))]
        public Dictionary<string, string> Properties { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);
    }

    internal class EgressProvider
    {
        [Display(
            ResourceType = typeof(SharedStrings),
            Description = nameof(SharedStrings.DisplayAttributeDescription_EgressProvider_EgressType))]
        //TODO This should honor DataMember, but only seems to work with JsonProperty
        [Newtonsoft.Json.JsonProperty("type", Required = Newtonsoft.Json.Required.Always)]
        public string EgressType { get; set; }

        [JsonExtensionData]
        public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
