namespace Kafka.Consumer.Console.InfraStructure.Consumers
{
    using Confluent.Kafka;
    using Kafka.Consumer.Console.Configuration;
    using Kafka.Consumer.Console.Contracts;
    using Microsoft.Extensions.Options;
    using System;
    using System.Threading;

    internal sealed class OrderConsumer : IOrderConsumer
    {
        private readonly IConsumer<string, string> _kafkaConsumer;

        public OrderConsumer(IOptions<KafkaConsumerOptions> options)
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

            _kafkaConsumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        }

        public void RegisterTopic(string topic)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(topic, nameof(topic));
            _kafkaConsumer.Subscribe(topic);
        }

        public ConsumeResult<string, string> Consume(CancellationToken stoppingToken)
        {
            return _kafkaConsumer.Consume(stoppingToken);
        }

        public void Commit(ConsumeResult<string, string> consumeResult)
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
