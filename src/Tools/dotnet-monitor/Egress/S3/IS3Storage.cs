// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System;
using Amazon.S3.Model;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.S3
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
