namespace Kafka.Common.InfraStructure.ProducerConsumer
{
    using Confluent.Kafka;
    using Confluent.Kafka.SyncOverAsync;
    using Confluent.SchemaRegistry;
    using Confluent.SchemaRegistry.Serdes;
    using Kafka.Common.Domain.Configuration;
    using Microsoft.Extensions.Options;
    using System;
    using System.Threading;

    public class BaseConsumer<TKey, TEvent> : IDisposable
    {
        private readonly IConsumer<TKey, TEvent> _topicConsumer;
        private bool _disposable;

        public BaseConsumer(IOptions<KafkaConsumerOptions> options, ISchemaRegistryClient schemaRegistry)
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

            _topicConsumer = new ConsumerBuilder<TKey, TEvent>(consumerConfig)
                .SetValueDeserializer(new AvroDeserializer<TEvent>(schemaRegistry).AsSyncOverAsync())
                .Build();

            _disposable = true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _disposable)
            {
                _disposable = false;
                _topicConsumer.Close();
                _topicConsumer.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BaseConsumer()
        {
            Dispose(false);
        }

        public void RegisterTopic(string topic)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(topic, nameof(topic));
            _topicConsumer.Subscribe(topic);
        }

        public ConsumeResult<TKey, TEvent> Consume(CancellationToken stoppingToken)
        {
            return _topicConsumer.Consume(stoppingToken);
        }

        public void Commit(ConsumeResult<TKey, TEvent> consumeResult)
        {
            _topicConsumer.Commit(consumeResult);
        }

        public void Close()
        {
            _topicConsumer.Close();
        }
    }
}
