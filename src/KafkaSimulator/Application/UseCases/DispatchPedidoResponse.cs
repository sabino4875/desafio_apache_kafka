namespace Kafka.Producer.Simulator.Application.UseCases
{
    using System;
    internal sealed record DispatchPedidoResponse(
    bool Success,
    string Detail,
    DateTimeOffset DispatchedAt);
}
