namespace Kafka.Consumer.Console.InfraStructure.Consumers
{
    using Confluent.SchemaRegistry;
    using Kafka.Common.Application.Events;
    using Kafka.Common.Domain.Configuration;
    using Kafka.Common.InfraStructure.ProducerConsumer;
    using Kafka.Consumer.Console.Contracts;
    using Microsoft.Extensions.Options;

    internal sealed class OrderConsumer : BaseConsumer<string, OrderCreatedEvent>, IOrderConsumer
    {
        public OrderConsumer(IOptions<KafkaConsumerOptions> options, ISchemaRegistryClient schemaRegistry)
            : base(options, schemaRegistry)
        {
        }
    }
}
