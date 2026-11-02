namespace Kafka.Producer.Api.EndPoints
{
    using Kafka.Common.Domain;
    using Kafka.Common.Dto;
    using Kafka.Producer.Api.Contracts;
    using Kafka.Producer.Api.Filters;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.RateLimiting;
    using System;
    using System.Threading;

    internal static class OrderEndPoints
    {
        public static WebApplication UseOrderEndPoints(this WebApplication app)
        {
            app.MapPost("/api/pedidos", async (
                [FromBody] CreateOrderRequest request,
                [FromServices] IOrderProducer producer,
                CancellationToken cancellationToken) =>
            {
                var orderEvent = new OrderCreatedEvent(
                    OrderId: Guid.NewGuid().ToString("N"),
                    ClientId: request.ClientId,
                    Total: request.Total,
                    Product: request.Product,
                    CreatedAt: DateTimeOffset.UtcNow
                );

                var result = await producer.PublishOrderAsync(orderEvent, cancellationToken);

                return Results.Ok(new
                {
                    Mensagem = result.Message,
                    Particao = result.Partition,
                    Offset = result.Offset,
                    OrderId = orderEvent.OrderId
                });
            })
            .AddEndpointFilter<ValidationFilter<CreateOrderRequest>>()
            .RequireRateLimiting("OrdersRatePolicy");

            return app;
        }
    }
}
