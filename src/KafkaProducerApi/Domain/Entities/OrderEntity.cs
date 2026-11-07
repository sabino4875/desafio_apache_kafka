namespace Kafka.Producer.Api.Domain.Entities
{
    using System;

    public sealed class OrderEntity
    {
        public string Id { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;

        public decimal Total { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
