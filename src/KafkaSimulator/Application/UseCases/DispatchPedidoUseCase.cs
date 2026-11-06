namespace Kafka.Producer.Simulator.Application.UseCases
{
    using Kafka.Producer.Simulator.Interfaces.Services;
    using Serilog;
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    internal sealed class DispatchPedidoUseCase(
    IPedidoDispatchService dispatchService,
    ILogger logger)
    {
        public async Task<DispatchPedidoResponse> ExecuteAsync(
            DispatchPedidoRequest request, CancellationToken ct)
        {
            var pedido = request;

            var products = pedido.Items.Select(e => string.Concat("Id: ", e.ProductId, "Name: ", e.ProductName, "Quantity: ", e.Quantity, "UnitPrice: ", e.UnitPrice, "ItemTotal: ", e.ItemTotal));

            logger.Information(
                "Despachando pedido | OrderId: {OrderId} | ClientId: {ClientId} | Valor: {Valor} | Criacao: {CreatedAt} | Produtos: {Products}",
                pedido.OrderId, pedido.ClientId, pedido.Total, pedido.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture), string.Join(" - ", products));

            var (success, detail, error) = await dispatchService.DispatchAsync(pedido, ct);

            if (!success)
                logger.Warning("Falha no despacho: [{Code}] {Message}",
                    error?.Code, error?.Message);

            return new DispatchPedidoResponse(success, detail, DateTimeOffset.UtcNow);
        }
    }
}
