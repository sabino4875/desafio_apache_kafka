namespace Kafka.Common.Diagnostics
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;

    public static class KafkaDiagnostics
    {
        public const string MeterName = "Kafka.Observability";
        public const string ActivitySourceName = "Kafka.Traces";

        public static readonly ActivitySource ActivitySource = new ActivitySource(ActivitySourceName);
        public static readonly Meter Meter = new Meter(MeterName, "1.0.0");

        // Producer I/O metrics
        private static readonly Counter<long> ProducerMessagesCounter = Meter.CreateCounter<long>(
            name: "kafka.producer.messages.count",
            unit: "{message}",
            description: "Total de mensagens publicadas pelo produtor Kafka");

        private static readonly Counter<long> ProducerBytesCounter = Meter.CreateCounter<long>(
            name: "kafka.producer.bytes.total",
            unit: "By",
            description: "Total de bytes produzidos e enviados ao Kafka");

        // Consumer I/O metrics
        private static readonly Counter<long> ConsumerMessagesCounter = Meter.CreateCounter<long>(
            name: "kafka.consumer.messages.count",
            unit: "{message}",
            description: "Total de mensagens consumidas e processadas pelos workers");

        private static readonly Counter<long> ConsumerBytesCounter = Meter.CreateCounter<long>(
            name: "kafka.consumer.bytes.total",
            unit: "By",
            description: "Total de bytes recebidos e processados pelos workers");

        // Lag storage for ObservableGauge
        private static readonly ConcurrentDictionary<string, long> PartitionLags = new ConcurrentDictionary<string, long>();
        private static long _maxLag;

        static KafkaDiagnostics()
        {
            Meter.CreateObservableGauge(
                name: "kafka.consumer.lag",
                observeValues: GetPartitionLags,
                unit: "{message}",
                description: "Consumer Lag por partição");

            Meter.CreateObservableGauge(
                name: "kafka.consumer.records_lag_max",
                observeValue: () => _maxLag,
                unit: "{message}",
                description: "Consumer Lag máximo entre todas as partições");
        }

        public static void RecordMessageProduced(string topic, int partition, long byteCount)
        {
            ArgumentNullException.ThrowIfNull(topic, nameof(topic));

            var tags = new TagList
            {
                { "messaging.system", "kafka" },
                { "messaging.destination.name", topic },
                { "messaging.destination.partition.id", partition }
            };

            ProducerMessagesCounter.Add(1, tags);
            ProducerBytesCounter.Add(byteCount, tags);
        }

        public static void RecordMessageConsumed(string topic, int partition, long byteCount)
        {
            ArgumentNullException.ThrowIfNull(topic, nameof(topic));

            var tags = new TagList
            {
                { "messaging.system", "kafka" },
                { "messaging.destination.name", topic },
                { "messaging.destination.partition.id", partition }
            };

            ConsumerMessagesCounter.Add(1, tags);
            ConsumerBytesCounter.Add(byteCount, tags);
        }

        public static void UpdatePartitionLag(string topic, int partition, long lag)
        {
            ArgumentNullException.ThrowIfNull(topic, nameof(topic));

            var key = $"{topic}-{partition}";
            PartitionLags[key] = lag;

            long currentMax = 0;
            foreach (var item in PartitionLags)
            {
                if (item.Value > currentMax)
                {
                    currentMax = item.Value;
                }
            }

            _maxLag = currentMax;
        }

        private static IEnumerable<Measurement<long>> GetPartitionLags()
        {
            var measurements = new List<Measurement<long>>(PartitionLags.Count);
            foreach (var kvp in PartitionLags)
            {
                var parts = kvp.Key.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[1], out var partition))
                {
                    var tags = new TagList
                    {
                        { "messaging.system", "kafka" },
                        { "messaging.destination.name", parts[0] },
                        { "messaging.destination.partition.id", partition }
                    };
                    measurements.Add(new Measurement<long>(kvp.Value, tags));
                }
                else
                {
                    measurements.Add(new Measurement<long>(kvp.Value));
                }
            }

            return measurements;
        }
    }
}
