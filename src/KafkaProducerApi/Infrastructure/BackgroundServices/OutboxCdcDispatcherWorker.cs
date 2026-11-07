namespace Kafka.Producer.Api.Infrastructure.BackgroundServices
{
    using Confluent.Kafka;
    using Dapper;
    using Kafka.Common.Application.Dto;
    using Kafka.Common.Application.Events;
    using Kafka.Common.Application.Profiles;
    using Kafka.Producer.Api.Contracts;
    using Kafka.Producer.Api.Domain.Entities;
    using Kafka.Producer.Api.Infrastructure.Database;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed partial class OutboxCdcDispatcherWorker : BackgroundService
    {
        private readonly IDbConnectionFactory connectionFactory;
        private readonly IOrderProducer producer;
        private readonly ILogger<OutboxCdcDispatcherWorker> logger;
        private readonly TimeSpan pollingInterval = TimeSpan.FromSeconds(2);

        public OutboxCdcDispatcherWorker(
            IDbConnectionFactory connectionFactory,
            IOrderProducer producer,
            ILogger<OutboxCdcDispatcherWorker> logger)
        {
            this.connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            this.producer = producer ?? throw new ArgumentNullException(nameof(producer));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            LogWorkerStarted(this.logger);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await this.ProcessPendingOutboxMessagesAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LogProcessingError(this.logger, ex);
                }

                await Task.Delay(this.pollingInterval, stoppingToken);
            }

            LogWorkerStopped(this.logger);
        }

        private async Task ProcessPendingOutboxMessagesAsync(CancellationToken cancellationToken)
        {
            await using var connection = await this.connectionFactory.CreateConnectionAsync(cancellationToken);

            const string selectSql = @"
                SELECT id AS Id, aggregate_type AS AggregateType, aggregate_id AS AggregateId,
                       type AS Type, payload AS Payload, created_at AS CreatedAt, processed_at AS ProcessedAt
                FROM outbox_messages
                WHERE processed_at IS NULL
                ORDER BY created_at ASC
                LIMIT 50;";

            var pendingMessages = await connection.QueryAsync<OutboxMessageEntity>(
                new CommandDefinition(selectSql, cancellationToken: cancellationToken));

            foreach (var message in pendingMessages)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    var orderRequest = JsonSerializer.Deserialize<CreateOrderRequest>(message.Payload);

                    if (orderRequest != null)
                    {
                        var orderEvent = new OrderCreatedEvent
                        {
                            OrderId = orderRequest.OrderId,
                            ClientId = orderRequest.ClientId,
                            Total = orderRequest.Total,
                            CreatedAt = orderRequest.CreatedAt
                        };

                        orderEvent.Items.Clear();
                        if (orderRequest.Items != null)
                        {
                            foreach (var item in orderRequest.Items)
                            {
                                orderEvent.Items.Add(item.ToEventItem());
                            }
                        }

                        await this.producer.PublishOrderAsync(orderEvent, cancellationToken);
                    }

                    const string markProcessedSql = @"
                        UPDATE outbox_messages
                        SET processed_at = @ProcessedAt
                        WHERE id = @Id;";

                    await connection.ExecuteAsync(new CommandDefinition(
                        markProcessedSql,
                        new { Id = message.Id, ProcessedAt = DateTime.UtcNow },
                        cancellationToken: cancellationToken));

                    LogMessageDispatched(this.logger, message.Id, message.AggregateId);
                }
                catch (Exception ex)
                {
                    LogMessageDispatchFailed(this.logger, message.Id, ex);
                }
            }
        }

        [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = "Iniciando Outbox CDC Dispatcher Worker...")]
        private static partial void LogWorkerStarted(ILogger logger);

        [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Outbox CDC Dispatcher Worker finalizado.")]
        private static partial void LogWorkerStopped(ILogger logger);

        [LoggerMessage(EventId = 12, Level = LogLevel.Information, Message = "Mensagem Outbox {OutboxId} para agregado {AggregateId} despachada com sucesso para o Kafka.")]
        private static partial void LogMessageDispatched(ILogger logger, Guid outboxId, string aggregateId);

        [LoggerMessage(EventId = 13, Level = LogLevel.Error, Message = "Erro ao processar lote de mensagens outbox.")]
        private static partial void LogProcessingError(ILogger logger, Exception ex);

        [LoggerMessage(EventId = 14, Level = LogLevel.Error, Message = "Falha ao despachar mensagem outbox {OutboxId} para o Kafka.")]
        private static partial void LogMessageDispatchFailed(ILogger logger, Guid outboxId, Exception ex);
    }
}
