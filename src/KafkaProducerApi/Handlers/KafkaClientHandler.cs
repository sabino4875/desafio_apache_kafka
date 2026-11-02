namespace Kafka.Producer.Api.Handlers
{
    using Confluent.Kafka;
    using Microsoft.Extensions.Configuration;
    using System;

    internal sealed class KafkaClientHandler : IDisposable
    {
        private readonly IProducer<byte[], byte[]> kafkaProducer;

        public KafkaClientHandler(IConfiguration config)
        {
            ArgumentNullException.ThrowIfNull(config, nameof(config));

            var conf = new ProducerConfig();
            config.GetSection("Kafka:ProducerSettings").Bind(conf);
            kafkaProducer = new ProducerBuilder<byte[], byte[]>(conf).Build();
        }

        public Handle Handle => kafkaProducer.Handle;

        public void Dispose()
        {
            kafkaProducer.Flush();
            kafkaProducer.Dispose();
        }
    }
}
