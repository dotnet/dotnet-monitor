// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.AzureEventHubsStorage
{
    internal sealed partial class AzureEventHubsEgressProviderOptions
    {
        [Required]
        public string ConnectionString { get; set; }

        [Required]
        public string EventHubName { get; set; }
    }
}
