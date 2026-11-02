namespace Kafka.Producer.Api.EndPoints
{
    using FluentValidation;
    using Kafka.Common.Application.Dto;
    using Kafka.Common.Application.Events;
    using Kafka.Producer.Api.Contracts;
    using Kafka.Producer.Api.Filters;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading;
    using Kafka.Common.Application.Profiles;
    using System.Linq;

    internal static class OrderEndPoints
    {
        public static WebApplication UseOrderEndPoints(this WebApplication app)
        {
            app.MapPost("/api/pedidos", async (
                [FromBody] CreateOrderRequest request,
                [FromServices] IOrderProducer producer,
                [FromServices] IValidator<CreateOrderRequest> validator,
                CancellationToken cancellationToken) =>
            {
                var validationResult = await validator.ValidateAsync(request, cancellationToken);

                if(validationResult.IsValid)
                {
                    var orderEvent = new OrderCreatedEvent {
                        OrderId = request.OrderId,
                        ClientId = request.ClientId,
                        Total = request.Total,
                        CreatedAt = request.CreatedAt,
                    };

                    orderEvent.Items.Clear();

                    foreach (var item in request.Items)
                    {
                        orderEvent.Items.Add(item.ToEventItem());
                    }


                    var result = await producer.PublishOrderAsync(orderEvent, cancellationToken);

                    return Results.Ok(new
                    {
                        result.Message,
                        result.Partition,
                        result.Offset,
                        orderEvent.OrderId
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
