namespace Kafka.Consumer.Console.InfraStructure.Consumers
{
    using Confluent.Kafka;
    using Confluent.Kafka.SyncOverAsync;
    using Confluent.SchemaRegistry;
    using Confluent.SchemaRegistry.Serdes;
    using Kafka.Common.Application.Events;
    using Kafka.Consumer.Console.Configuration;
    using Kafka.Consumer.Console.Contracts;
    using Microsoft.Extensions.Options;
    using System;
    using System.Threading;

    internal sealed class OrderConsumer : IOrderConsumer
    {
        private readonly IConsumer<string, OrderCreatedEvent> _kafkaConsumer;

        public OrderConsumer(IOptions<KafkaConsumerOptions> options, ISchemaRegistryClient schemaRegistry)
        {
            ArgumentNullException.ThrowIfNull(options, nameof(options));

            var settings = options.Value.ConsumerSettings;
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = settings.BootstrapServers,
                GroupId = settings.GroupId,
                EnableAutoOffsetStore = settings.EnableAutoOffsetStore,
                EnableAutoCommit = settings.EnableAutoCommit,
                StatisticsIntervalMs = settings.StatisticsIntervalMs,
                SessionTimeoutMs = settings.SessionTimeoutMs,
                EnablePartitionEof = settings.EnablePartitionEof,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky
            };

            _kafkaConsumer = new ConsumerBuilder<string, OrderCreatedEvent>(consumerConfig)
                .SetValueDeserializer(new AvroDeserializer<OrderCreatedEvent>(schemaRegistry).AsSyncOverAsync())
                .Build();
        }

        public void RegisterTopic(string topic)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(topic, nameof(topic));
            _kafkaConsumer.Subscribe(topic);
        }

        public ConsumeResult<string, OrderCreatedEvent> Consume(CancellationToken stoppingToken)
        {
            return _kafkaConsumer.Consume(stoppingToken);
        }

        public void Commit(ConsumeResult<string, OrderCreatedEvent> consumeResult)
        {
            _kafkaConsumer.Commit(consumeResult);
        }

        public void Close()
        {
            _kafkaConsumer.Close();
        }

        public void Dispose()
        {
            _kafkaConsumer.Close();
            _kafkaConsumer.Dispose();
        }
    }
}
