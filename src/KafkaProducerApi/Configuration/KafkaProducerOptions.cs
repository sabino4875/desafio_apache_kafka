namespace Kafka.Producer.Api.Configuration
{
    using System.ComponentModel.DataAnnotations;

    internal sealed class KafkaProducerOptions
    {
        public const string Section = "Kafka";

        [Required(ErrorMessage = "O nome do tópico de pedidos é obrigatório.")]
        public string OrdersTopic { get; set; } = "pedidos-vendas";

        public ProducerSettingsOptions ProducerSettings { get; set; } = new();
    }

    internal sealed class ProducerSettingsOptions
    {
        public string BootstrapServers { get; set; } = "localhost:9092";
    }
}
