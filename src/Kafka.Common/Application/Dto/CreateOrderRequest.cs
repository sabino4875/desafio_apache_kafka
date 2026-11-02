namespace Kafka.Common.Application.Dto
{
    using System;
    using System.Collections.ObjectModel;
    public record CreateOrderRequest(
        string OrderId,
        string ClientId,
        decimal Total,
        DateTime CreatedAt,
        Collection<OrderItemDto> Items
    );
}
