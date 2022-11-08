// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.Egress.AzureBlob;
using Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem;
using Microsoft.Diagnostics.Tools.Monitor.Egress.S3;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class EgressOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_EgressOptions_AzureBlobStorage))]
        public IDictionary<string, AzureBlobEgressProviderOptions> AzureBlobStorage { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_EgressOptions_S3Storage))]
        public IDictionary<string, S3StorageEgressProviderOptions> S3Storage { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_EgressOptions_FileSystem))]
        public IDictionary<string, FileSystemEgressProviderOptions> FileSystem { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_EgressOptions_Properties))]
        public IDictionary<string, string> Properties { get; set; }
    }
}
