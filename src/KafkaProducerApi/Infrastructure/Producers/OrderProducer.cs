namespace Kafka.Producer.Api.Infrastructure.Producers
{
    using Confluent.Kafka;
    using Confluent.SchemaRegistry;
    using Kafka.Common.Application.Events;
    using Kafka.Common.Domain.Configuration;
    using Kafka.Common.InfraStructure.ProducerConsumer;
    using Kafka.Producer.Api.Contracts;
    using Microsoft.Extensions.Options;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class OrderProducer : BaseProducer<string, OrderCreatedEvent, OrderPublishResult>, IOrderProducer
    {
        public OrderProducer(IOptions<KafkaProducerOptions> options, ISchemaRegistryClient schemaRegistryClient)
            : base(options, schemaRegistryClient)
        {
        }

        public Task<OrderPublishResult> PublishOrderAsync(OrderCreatedEvent orderEvent, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(orderEvent, nameof(orderEvent));
            return PublishOrderAsync(orderEvent.ClientId, orderEvent, cancellationToken);
        }

        protected override OrderPublishResult OperationResult(DeliveryResult<string, OrderCreatedEvent> topicEvent)
        {
            ArgumentNullException.ThrowIfNull(topicEvent, nameof(topicEvent));

            return new OrderPublishResult(
                Success: true,
                Topic: topicEvent.Topic,
                Partition: topicEvent.Partition.Value,
                Offset: topicEvent.Offset.Value,
                Message: "Pedido publicado no Kafka com sucesso!"
            );
        }
    }
}
