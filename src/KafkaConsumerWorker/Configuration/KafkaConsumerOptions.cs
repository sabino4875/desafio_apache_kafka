namespace Kafka.Consumer.Console.Configuration
{
    using System.ComponentModel.DataAnnotations;

    internal sealed class KafkaConsumerOptions
    {
        public const string Section = "Kafka";

        [Required(ErrorMessage = "O nome do tópico de pedidos é obrigatório.")]
        public string OrdersTopic { get; set; } = "pedidos-vendas";

        public ConsumerSettingsOptions ConsumerSettings { get; set; } = new();
    }

    internal sealed class ConsumerSettingsOptions
    {
        public string BootstrapServers { get; set; } = "localhost:9092";
        public string GroupId { get; set; } = "pedido-vendas-consumer";
        public bool EnableAutoOffsetStore { get; set; }
        public bool EnableAutoCommit { get; set; } = true;
        public int StatisticsIntervalMs { get; set; } = 5000;
        public int SessionTimeoutMs { get; set; } = 6000;
        public bool EnablePartitionEof { get; set; } = true;
    }
}
