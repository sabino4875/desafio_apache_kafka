namespace Kafka.Common.InfraStructure.ProducerConsumer
{
    using Confluent.Kafka;
    using Confluent.Kafka.SyncOverAsync;
    using Confluent.SchemaRegistry;
    using Confluent.SchemaRegistry.Serdes;
    using Kafka.Common.Diagnostics;
    using Kafka.Common.Domain.Configuration;
    using Microsoft.Extensions.Options;
    using System;
    using System.Text.Json;
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
                StatisticsIntervalMs = settings.StatisticsIntervalMs > 0 ? settings.StatisticsIntervalMs : 5000,
                SessionTimeoutMs = settings.SessionTimeoutMs,
                EnablePartitionEof = settings.EnablePartitionEof,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky
            };

            _topicConsumer = new ConsumerBuilder<TKey, TEvent>(consumerConfig)
                .SetValueDeserializer(new AvroDeserializer<TEvent>(schemaRegistry).AsSyncOverAsync())
                .SetStatisticsHandler(OnStatisticsReceived)
                .Build();

            _disposable = true;
        }

        private static void OnStatisticsReceived(IConsumer<TKey, TEvent> consumer, string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            try
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (!root.TryGetProperty("topics", out var topicsElement))
                {
                    return;
                }

                foreach (var topicProperty in topicsElement.EnumerateObject())
                {
                    var topicName = topicProperty.Name;
                    var topicValue = topicProperty.Value;

                    if (!topicValue.TryGetProperty("partitions", out var partitionsElement))
                    {
                        continue;
                    }

                    foreach (var partitionProperty in partitionsElement.EnumerateObject())
                    {
                        if (!int.TryParse(partitionProperty.Name, out var partitionId) || partitionId < 0)
                        {
                            continue;
                        }

                        var partitionValue = partitionProperty.Value;
                        if (partitionValue.TryGetProperty("consumer_lag", out var lagElement) &&
                            lagElement.TryGetInt64(out var lag))
                        {
                            if (lag < 0)
                            {
                                lag = 0;
                            }

                            KafkaDiagnostics.UpdatePartitionLag(topicName, partitionId, lag);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Ignora erros de parse pontuais para não impactar a execução do consumer
            }
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
