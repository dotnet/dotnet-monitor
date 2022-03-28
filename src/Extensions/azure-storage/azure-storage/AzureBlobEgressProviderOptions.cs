
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.AzureStorage
{
    internal sealed class AzureBlobEgressProviderOptions 
    {
        [Required]
        public Uri AccountUri { get; set; }

        public string AccountKey { get; set; }

        public string AccountKeyName { get; set; }

        public string SharedAccessSignature { get; set; }

        public string SharedAccessSignatureName { get; set; }

        [Required]
        public string ContainerName { get; set; }

        public string BlobPrefix { get; set; }

        public int? CopyBufferSize { get; set; }

        public string QueueName { get; set; }

        public Uri QueueAccountUri { get; set; }
    }
}
