// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal interface IEgressService
    {
        void ValidateProviderExists(string providerName);

        Task<EgressResult> EgressAsync(
            string providerName,
            Func<Stream, CancellationToken, Task> action,
            string fileName,
            string contentType,
            IEndpointInfo source,
            CollectionRuleMetadata? collectionRuleMetadata,
            CancellationToken token);

        Task ValidateProviderOptionsAsync(
            string providerName,
            CancellationToken token);
    }
}
