namespace Kafka.Producer.Api.Domain.Entities
{
    using System;

    internal sealed class OutboxMessageEntity
    {
        public Guid Id { get; set; }

        public string AggregateType { get; set; } = string.Empty;

        public string AggregateId { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public string Payload { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }
    }
}
