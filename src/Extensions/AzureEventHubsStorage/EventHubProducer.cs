// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace Microsoft.Diagnostics.Monitoring.AzureEventHubsStorage
{
    internal sealed class EventHubProducer : IEventHubProducer
    {
        private readonly EventHubProducerClient _producerClient;
        private readonly string _eventHubName;

        public EventHubProducer(EventHubProducerClient client, string eventHubName)
        {
            _producerClient = client;
            _eventHubName = eventHubName;
        }

        public async Task ProduceAsync(Stream inputStream, CancellationToken cancellationToken = default)
        {
            if (inputStream is not null && inputStream.CanSeek && inputStream.Position != 0L)
            {
                inputStream.Seek(0L, SeekOrigin.Begin);
            }

            EventData eventData;

            if (inputStream is MemoryStream memoryStream)
            {
                var inputBytes = memoryStream.ToArray();

                eventData = new EventData(inputBytes);
            }
            else
            {
                using (var ms = new MemoryStream())
                {
                    inputStream.CopyTo(ms);
                    var msBytes = ms.ToArray();

                    eventData = new EventData(msBytes);
                }
            }

            var eventBatch = await _producerClient.CreateBatchAsync(new CreateBatchOptions(), cancellationToken).ConfigureAwait(false);

            eventBatch.TryAdd(eventData);

            await _producerClient.SendAsync(eventBatch, cancellationToken).ConfigureAwait(false);

            eventBatch.Dispose();
        }

        public static IEventHubProducer Create(AzureEventHubsEgressProviderOptions options)
        {
            var client = new EventHubProducerClient(options.ConnectionString, options.EventHubName);

            return new EventHubProducer(client, options.EventHubName);
        }
    }
}
