// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.Extension.S3Storage
{
    /// <summary>
    /// Egress provider options for S3 storage.
    /// </summary>
    internal sealed partial class S3StorageEgressProviderOptions
    {
        public string Endpoint { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string BucketName { get; set; }

        public string RegionName { get; set; }

        public string AccessKeyId { get; set; }

        public string SecretAccessKey { get; set; }

        public string AwsProfileName { get; set; }

        public string AwsProfilePath { get; set; }

        [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00")]
        public TimeSpan? PreSignedUrlExpiry { get; set; }

        public bool ForcePathStyle { get; set; }

        [Range(1, int.MaxValue)]
        public int? CopyBufferSize { get; set; }
    }
}
