namespace Kafka.Producer.Api.EndPoints
{
    using FluentValidation;
    using Kafka.Common.Application.Dto;
    using Kafka.Producer.Api.Contracts;
    using Kafka.Producer.Api.Domain.Entities;
    using Kafka.Producer.Api.Filters;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;

    internal static class OrderEndPoints
    {
        public static WebApplication UseOrderEndPoints(this WebApplication app)
        {
            app.MapPost("/api/pedidos", async (
                [FromBody] CreateOrderRequest request,
                [FromServices] IOrderRepository orderRepository,
                [FromServices] IValidator<CreateOrderRequest> validator,
                CancellationToken cancellationToken) =>
            {
                var validationResult = await validator.ValidateAsync(request, cancellationToken);

                if (validationResult.IsValid)
                {
                    var order = new OrderEntity
                    {
                        Id = request.OrderId,
                        ClientId = request.ClientId,
                        Total = request.Total,
                        CreatedAt = request.CreatedAt
                    };

                    var items = new List<OrderItemEntity>();
                    foreach (var item in request.Items)
                    {
                        items.Add(new OrderItemEntity
                        {
                            Id = Guid.NewGuid().ToString("D"),
                            OrderId = request.OrderId,
                            ProductId = item.ProductId,
                            ProductName = item.ProductName,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            ItemTotal = item.ItemTotal
                        });
                    }

                    var outboxPayload = JsonSerializer.Serialize(request);

                    var outbox = new OutboxMessageEntity
                    {
                        Id = Guid.NewGuid(),
                        AggregateType = "Order",
                        AggregateId = request.OrderId,
                        Type = "OrderCreated",
                        Payload = outboxPayload,
                        CreatedAt = DateTime.UtcNow,
                        ProcessedAt = null
                    };

                    await orderRepository.CreateOrderWithOutboxAsync(order, items, outbox, cancellationToken);

                    return Results.Ok(new
                    {
                        Message = "Pedido e mensagem outbox persistidos com sucesso.",
                        order.Id,
                        OutboxId = outbox.Id,
                        Status = "PersistedWithOutbox"
                    });
                }

                var mappingErrors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        grupo => grupo.Key,
                        grupo => grupo.Select(e => e.ErrorMessage).ToArray()
                    );

                return Results.ValidationProblem(
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    errors: mappingErrors,
                    detail: "Um ou mais campos enviados não atendem às regras de validação.",
                    title: "Falha na validação do pedido"
                );
            })
            .AddEndpointFilter<ValidationFilter<CreateOrderRequest>>()
            .RequireRateLimiting("OrdersRatePolicy");

            return app;
        }
    }
}
