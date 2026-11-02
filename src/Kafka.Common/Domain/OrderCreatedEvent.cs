namespace Kafka.Common.Domain
{
    using System;

    public record OrderCreatedEvent(
        string OrderId,
        string ClientId,
        decimal Total,
        string Product,
        DateTimeOffset CreatedAt
    );
}
