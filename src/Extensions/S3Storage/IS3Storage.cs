// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.Extension.S3Storage
{
    public interface IS3Storage
    {
        Task PutAsync(Stream inputStream, CancellationToken token);
        Task UploadAsync(Stream inputStream, CancellationToken token);
        Task<PartETag> UploadPartAsync(string uploadId, int partNumber, int partSize, Stream inputStream, CancellationToken token);
        Task AbortMultipartUploadAsync(string uploadId, CancellationToken cancellationToken);
        Task CompleteMultiPartUploadAsync(string uploadId, List<PartETag> parts, CancellationToken cancellationToken);
        Task<string> InitMultiPartUploadAsync(IDictionary<string, string> metadata, CancellationToken cancellationToken);
        string GetTemporaryResourceUrl(DateTime expires);
    }
}
