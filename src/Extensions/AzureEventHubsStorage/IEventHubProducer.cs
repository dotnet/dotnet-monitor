// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.AzureEventHubsStorage
{
    public interface IEventHubProducer
    {
        Task ProduceAsync(Stream inputStream, CancellationToken cancellationToken = default);
    }
}
