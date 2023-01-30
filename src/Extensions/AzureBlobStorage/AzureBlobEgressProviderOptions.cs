// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.AzureBlobStorage
{
    /// <summary>
    /// Egress provider options for Azure blob storage.
    /// </summary>
    internal sealed partial class AzureBlobEgressProviderOptions
    {
        [Required]
        public Uri AccountUri { get; set; }

        public string AccountKey { get; set; }

        public string AccountKeyName { get; set; }

        public string SharedAccessSignature { get; set; }

        public string SharedAccessSignatureName { get; set; }

        public string ManagedIdentityClientId { get; set; }

        [Required]
        public string ContainerName { get; set; }

        public string BlobPrefix { get; set; }

        public int? CopyBufferSize { get; set; }

        public string QueueName { get; set; }

        public Uri QueueAccountUri { get; set; }

        public string QueueSharedAccessSignature { get; set; }

        public string QueueSharedAccessSignatureName { get; set; }

        public IDictionary<string, string> Metadata { get; set; }
            = new Dictionary<string, string>(0);
    }
}
