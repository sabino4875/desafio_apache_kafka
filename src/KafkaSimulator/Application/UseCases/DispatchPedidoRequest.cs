namespace Kafka.Producer.Simulator.Application.UseCases
{
    internal sealed record DispatchPedidoRequest(
     string ClientId,
     decimal ValorTotal,
     string Item);
}
