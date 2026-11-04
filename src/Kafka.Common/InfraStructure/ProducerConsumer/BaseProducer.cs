namespace Kafka.Common.InfraStructure.ProducerConsumer
{
    using Avro;
    using Confluent.Kafka;
    using Confluent.SchemaRegistry;
    using Confluent.SchemaRegistry.Serdes;
    using Kafka.Common.Domain.Configuration;
    using Microsoft.Extensions.Options;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class BaseProducer<TKey, TEvent, TResult> : IDisposable
    {
        private readonly IProducer<TKey, TEvent> _topicProducer;
        private readonly KafkaProducerOptions _options;
        private bool _disposable;

        protected BaseProducer(IOptions<KafkaProducerOptions> options, ISchemaRegistryClient schemaRegistryClient)
        {
            ArgumentNullException.ThrowIfNull(options, nameof(options));
            ArgumentNullException.ThrowIfNull(schemaRegistryClient, nameof(schemaRegistryClient));

            var config = new ProducerConfig
            {
                // User-specific properties that you must set
                BootstrapServers =options.Value.ProducerSettings.BootstrapServers,
                AllowAutoCreateTopics = true,
                //SaslUsername = "<CLUSTER API KEY>",
                //SaslPassword = "<CLUSTER API SECRET>",

                // Fixed properties
                //SecurityProtocol = SecurityProtocol.SaslSsl,
                //SaslMechanism = SaslMechanism.Plain,
                Acks = Acks.All
            };

            _topicProducer = new ProducerBuilder<TKey, TEvent>(config)
                .SetValueSerializer(new AvroSerializer<TEvent>(schemaRegistryClient))
                .Build();

            _options = options.Value;
            _disposable = true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if(disposing && _disposable)
            {
                _disposable = false;
                _topicProducer.Flush();
                _topicProducer.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BaseProducer()
        {
            Dispose(false);
        }

        protected abstract TResult OperationResult(DeliveryResult<TKey, TEvent> topicEvent);

        public async Task<TResult> PublishOrderAsync(TKey keyEvent, TEvent topicEvent, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(keyEvent, nameof(keyEvent));
            ArgumentNullException.ThrowIfNull(topicEvent, nameof(topicEvent));

            var topic = _options.TopicName;
            var payload = topicEvent;

            var message = new Message<TKey, TEvent>
            {
                Key = keyEvent,
                Value = payload
            };

            var result = await _topicProducer.ProduceAsync(topic, message, cancellationToken);

            return OperationResult(result);
            /*
             bool success,
             string topic,
             int partition,
             long offset,
             string message,
             */
        }

        public Task<DeliveryResult<TKey, TEvent>> ProduceAsync(string topic, Message<TKey, TEvent> message)
            => _topicProducer.ProduceAsync(topic, message);

        public void Produce(string topic, Message<TKey, TEvent> message, Action<DeliveryReport<TKey, TEvent>> deliveryHandler = null)
            => _topicProducer.Produce(topic, message, deliveryHandler);

        public void Flush(TimeSpan timeout)
            => _topicProducer.Flush(timeout);
    }
}
