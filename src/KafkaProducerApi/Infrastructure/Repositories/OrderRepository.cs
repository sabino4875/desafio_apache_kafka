namespace Kafka.Producer.Api.Infrastructure.Repositories
{
    using Dapper;
    using Kafka.Producer.Api.Contracts;
    using Kafka.Producer.Api.Domain.Entities;
    using Kafka.Producer.Api.Infrastructure.Database;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class OrderRepository : IOrderRepository
    {
        private readonly IDbConnectionFactory connectionFactory;

        public OrderRepository(IDbConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task CreateOrderWithOutboxAsync(
            OrderEntity order,
            IEnumerable<OrderItemEntity> items,
            OutboxMessageEntity outboxMessage,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(order);
            ArgumentNullException.ThrowIfNull(items);
            ArgumentNullException.ThrowIfNull(outboxMessage);

            await using var connection = await this.connectionFactory.CreateConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                const string insertOrderSql = @"
                    INSERT INTO orders (id, client_id, total, created_at)
                    VALUES (@Id, @ClientId, @Total, @CreatedAt);";

                var orderCmd = new CommandDefinition(
                    insertOrderSql,
                    new { order.Id, order.ClientId, order.Total, order.CreatedAt },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                await connection.ExecuteAsync(orderCmd);

                const string insertOrderItemSql = @"
                    INSERT INTO order_items (id, order_id, product_id, product_name, quantity, unit_price, item_total)
                    VALUES (@Id, @OrderId, @ProductId, @ProductName, @Quantity, @UnitPrice, @ItemTotal);";

                foreach (var item in items)
                {
                    var itemCmd = new CommandDefinition(
                        insertOrderItemSql,
                        new
                        {
                            item.Id,
                            item.OrderId,
                            item.ProductId,
                            item.ProductName,
                            item.Quantity,
                            item.UnitPrice,
                            item.ItemTotal
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken);

                    await connection.ExecuteAsync(itemCmd);
                }

                const string insertOutboxSql = @"
                    INSERT INTO outbox_messages (id, aggregate_type, aggregate_id, type, payload, created_at, processed_at)
                    VALUES (@Id, @AggregateType, @AggregateId, @Type, @Payload, @CreatedAt, @ProcessedAt);";

                var outboxCmd = new CommandDefinition(
                    insertOutboxSql,
                    new
                    {
                        outboxMessage.Id,
                        outboxMessage.AggregateType,
                        outboxMessage.AggregateId,
                        outboxMessage.Type,
                        outboxMessage.Payload,
                        outboxMessage.CreatedAt,
                        outboxMessage.ProcessedAt
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                await connection.ExecuteAsync(outboxCmd);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
