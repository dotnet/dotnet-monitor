// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure;
using Microsoft.Diagnostics.Monitoring.Extension.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Monitoring.AzureEventHubsStorage
{
    internal sealed partial class AzureEventHubsEgressProvider : EgressProvider<AzureEventHubsEgressProviderOptions>
    {
        private readonly ILogger _logger;

#pragma warning disable CA1852
        internal class StorageFactory
        {
            public virtual IEventHubProducer Create(AzureEventHubsEgressProviderOptions options, EgressArtifactSettings settings, CancellationToken cancellationToken) => EventHubProducer.Create(options);
        }
#pragma warning restore CA1852

        internal StorageFactory ClientFactory = new();

        public AzureEventHubsEgressProvider(ILogger<AzureEventHubsEgressProvider> logger)
        {
            _logger = logger;
        }

        public override async Task<string> EgressAsync(
            AzureEventHubsEgressProviderOptions options,
            Func<Stream, CancellationToken, Task> action,
            EgressArtifactSettings artifactSettings,
            CancellationToken token)
        {
            try
            {
                IEventHubProducer producer = null;
                using (var memoryStream = new MemoryStream())
                {
                    _logger.LogDebug("Provider {ProviderType}: Invoking stream action.", Constants.AzureEventHubsStorageProviderName);

                    await action(memoryStream, token);

                    memoryStream.Position = 0;

                    producer = ClientFactory.Create(options, artifactSettings, token);

                    await producer.ProduceAsync(memoryStream, token);
                }

                return null;
            }
            catch (AggregateException ex) when (ex.InnerException is RequestFailedException innerException)
            {
                throw CreateException(innerException);
            }
            catch (RequestFailedException ex)
            {
                throw CreateException(ex);
            }
        }

        private static EgressException CreateException(Exception innerException)
        {
            return new EgressException(innerException.Message, innerException);
        }
    }
}
