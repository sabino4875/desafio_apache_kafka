namespace Kafka.Common.Domain.Configuration
{
    using System.ComponentModel.DataAnnotations;

    public sealed class KafkaProducerOptions
    {
        public const string Section = "Kafka";

        [Required(ErrorMessage = "O nome do tópico de pedidos é obrigatório.")]
        public string TopicName { get; set; } = "pedidos-vendas";

        public ProducerSettingsOptions ProducerSettings { get; set; } = new();
    }

    public sealed class ProducerSettingsOptions
    {
        public string BootstrapServers { get; set; } = "localhost:9092";

        public bool EnableIdempotence { get; set; } = true;
    }
}
