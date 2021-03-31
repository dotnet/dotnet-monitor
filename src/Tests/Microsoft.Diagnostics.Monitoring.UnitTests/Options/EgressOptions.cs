// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.UnitTests.Options
{
    internal class EgressOptions
    {
        [Description("Named providers for egress. The names can be referenced when requesting artifacts, such as dumps or traces.")]
        public Dictionary<string, EgressProvider> Providers { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        [Description("Additional properties, such as secrets, that can be referenced by the provider definitions.")]
        public Dictionary<string, string> Properties { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);
    }

    internal class EgressProvider
    {
        [Description("The type of provider. Currently this supports fileSystem and azureBlobStorage.")]
        //TODO This should honor DataMember, but only seems to work with JsonProperty
        [Newtonsoft.Json.JsonProperty("type", Required = Newtonsoft.Json.Required.Always)]
        public string EgressType { get; set; }

        [JsonExtensionData]
        public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
