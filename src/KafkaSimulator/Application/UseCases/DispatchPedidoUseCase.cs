namespace Kafka.Producer.Simulator.Application.UseCases
{
    using Kafka.Common.Dto;
    using Kafka.Producer.Simulator.Domain;
    using Kafka.Producer.Simulator.Interfaces.Services;
    using Serilog;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    internal sealed class DispatchPedidoUseCase(
    IPedidoDispatchService dispatchService,
    ILogger logger)
    {
        public async Task<DispatchPedidoResponse> ExecuteAsync(
            DispatchPedidoRequest request, CancellationToken ct)
        {
            var pedido = new CreateOrderRequest(request.ClientId, request.ValorTotal, request.Item);

            logger.Information(
                "Despachando pedido | ClientId: {ClientId} | Product: {Item} | Valor: {Valor}",
                pedido.ClientId, pedido.Product, pedido.Total);

            var (success, detail, error) = await dispatchService.DispatchAsync(pedido, ct);

            if (!success)
                logger.Warning("Falha no despacho: [{Code}] {Message}",
                    error?.Code, error?.Message);

            return new DispatchPedidoResponse(success, detail, DateTimeOffset.UtcNow);
        }
    }
}
