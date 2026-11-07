namespace Kafka.Producer.Api.Contracts
{
    using Kafka.Producer.Api.Domain.Entities;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    internal interface IOrderRepository
    {
        Task CreateOrderWithOutboxAsync(
            OrderEntity order,
            IEnumerable<OrderItemEntity> items,
            OutboxMessageEntity outboxMessage,
            CancellationToken cancellationToken = default);
    }
}
