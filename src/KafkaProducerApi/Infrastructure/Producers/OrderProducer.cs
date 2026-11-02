namespace Kafka.Producer.Api.Infrastructure.Producers
{
    using Confluent.Kafka;
    using Kafka.Common.Domain;
    using Kafka.Producer.Api.Configuration;
    using Kafka.Producer.Api.Contracts;
    using Kafka.Producer.Api.Handlers;
    using Microsoft.Extensions.Options;
    using System;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class OrderProducer : IOrderProducer
    {
        private readonly IProducer<string, string> _topicProducer;
        private readonly KafkaProducerOptions _options;

        public OrderProducer(KafkaClientHandler handle, IOptions<KafkaProducerOptions> options)
        {
            ArgumentNullException.ThrowIfNull(handle, nameof(handle));
            ArgumentNullException.ThrowIfNull(options, nameof(options));

            _topicProducer = new DependentProducerBuilder<string, string>(handle.Handle).Build();
            _options = options.Value;
        }

        public async Task<OrderPublishResult> PublishOrderAsync(OrderCreatedEvent orderEvent, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(orderEvent, nameof(orderEvent));

            var topic = _options.OrdersTopic;
            var payload = JsonSerializer.Serialize(orderEvent);

            var message = new Message<string, string>
            {
                Key = orderEvent.ClientId,
                Value = payload
            };

            var result = await _topicProducer.ProduceAsync(topic, message, cancellationToken);

            return new OrderPublishResult(
                Success: true,
                Topic: result.Topic,
                Partition: result.Partition.Value,
                Offset: result.Offset.Value,
                Message: "Pedido publicado no Kafka com sucesso!"
            );
        }

        public Task<DeliveryResult<string, string>> ProduceAsync(string topic, Message<string, string> message)
            => _topicProducer.ProduceAsync(topic, message);

        public void Produce(string topic, Message<string, string> message, Action<DeliveryReport<string, string>> deliveryHandler = null)
            => _topicProducer.Produce(topic, message, deliveryHandler);

        public void Flush(TimeSpan timeout)
            => _topicProducer.Flush(timeout);
    }
}
